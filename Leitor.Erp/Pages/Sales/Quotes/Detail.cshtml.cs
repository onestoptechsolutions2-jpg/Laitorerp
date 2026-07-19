using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class DetailModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;
    private readonly QuoteLineAppService _quoteLineAppService;
    private readonly ProductAppService _productAppService;
    private readonly TaxRateAppService _taxRateAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyOptions _companyOptions;

    public DetailModel(
        QuoteAppService quoteAppService,
        QuoteLineAppService quoteLineAppService,
        ProductAppService productAppService,
        TaxRateAppService taxRateAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IEmailSender emailSender,
        IOptions<ErpCompanyOptions> companyOptions)
    {
        _quoteAppService = quoteAppService;
        _quoteLineAppService = quoteLineAppService;
        _productAppService = productAppService;
        _taxRateAppService = taxRateAppService;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _emailSender = emailSender;
        _companyOptions = companyOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public QuoteDto Quote { get; set; } = null!;
    public IReadOnlyList<QuoteLineDto> Lines { get; set; } = Array.Empty<QuoteLineDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public List<SelectListItem> TaxRateOptions { get; set; } = new();
    public Customer Customer { get; set; } = null!;

    [BindProperty]
    public CreateUpdateQuoteLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    public bool CanEdit { get; set; }

    // Once a Quote already has an Order (or is Rejected/Expired), attempting the conversion again
    // would just throw - hide the button and show a link to the existing Order instead, so the
    // quote reads as effectively read-only for this action rather than dead-ending in an error.
    public Guid? ExistingOrderId { get; set; }
    public bool CanConvertToOrder => !ExistingOrderId.HasValue && Quote.Status is not (QuoteStatus.Rejected or QuoteStatus.Expired);

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Quote = await _quoteAppService.GetAsync(Id);
        Customer = await _customerRepository.GetAsync(Quote.CustomerId);

        var existingOrder = (await _orderRepository.GetListAsync(x => x.QuoteId == Id)).FirstOrDefault();
        ExistingOrderId = existingOrder?.Id;

        var lines = await _quoteLineAppService.GetListAsync(new GetQuoteLineListInput
        {
            QuoteId = Id,
            MaxResultCount = 1000
        });
        Lines = lines.Items;

        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            IsActive = true,
            MaxResultCount = 1000
        });
        ProductOptions = new List<SelectListItem> { new(L["None"], "") };
        ProductOptions.AddRange(
            products.Items.OrderBy(x => x.Name).Select(x => new SelectListItem($"{x.Name} ({x.UnitPrice:N2})", x.Id.ToString()))
        );

        var taxRates = await _taxRateAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        TaxRateOptions = new List<SelectListItem> { new(L["UseDefaultTaxRate"], "") };
        TaxRateOptions.AddRange(
            taxRates.Items.OrderBy(x => x.Name).Select(x => new SelectListItem($"{x.Name} ({x.Percent:N0}%)", x.Id.ToString()))
        );
    }

    public async Task<IActionResult> OnPostAddLineAsync()
    {
        NewLine.QuoteId = Id;
        await _quoteLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _quoteLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostConvertToOrderAsync()
    {
        // Defends against a double-click/back-button resubmit/second tab hitting this after the
        // button should have been hidden - redirect to the existing Order instead of letting
        // ConvertToOrderAsync's guard throw into a raw error page.
        var existingOrder = (await _orderRepository.GetListAsync(x => x.QuoteId == Id)).FirstOrDefault();
        if (existingOrder != null)
        {
            return RedirectToPage("/Sales/Orders/Detail", new { id = existingOrder.Id });
        }

        var order = await _quoteAppService.ConvertToOrderAsync(Id);
        return RedirectToPage("/Sales/Orders/Detail", new { id = order.Id });
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadAsync();
        var pdfBytes = QuotePdfDocument.Generate(Quote, Lines, Customer, _companyOptions);
        return File(pdfBytes, "application/pdf", $"{Quote.QuoteNumber}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        await LoadAsync();

        if (!string.IsNullOrWhiteSpace(Customer.Email))
        {
            var pdfBytes = QuotePdfDocument.Generate(Quote, Lines, Customer, _companyOptions);
            await _emailSender.SendAsync(
                Customer.Email,
                $"Quote {Quote.QuoteNumber}",
                $"Dear {Customer.Name},\n\nPlease find attached quote {Quote.QuoteNumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{Quote.QuoteNumber}.pdf", File = pdfBytes }
                    }
                }
            );
        }

        return RedirectToPage(new { id = Id });
    }
}
