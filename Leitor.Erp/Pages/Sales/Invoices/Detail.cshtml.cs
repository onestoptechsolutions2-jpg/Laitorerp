using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.Sales.Invoices;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class DetailModel : AbpPageModel
{
    private readonly InvoiceAppService _invoiceAppService;
    private readonly InvoiceLineAppService _invoiceLineAppService;
    private readonly PaymentAppService _paymentAppService;
    private readonly ProductAppService _productAppService;
    private readonly TaxRateAppService _taxRateAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyOptions _companyOptions;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        InvoiceAppService invoiceAppService,
        InvoiceLineAppService invoiceLineAppService,
        PaymentAppService paymentAppService,
        ProductAppService productAppService,
        TaxRateAppService taxRateAppService,
        IRepository<Customer, Guid> customerRepository,
        IEmailSender emailSender,
        IOptions<ErpCompanyOptions> companyOptions,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _invoiceAppService = invoiceAppService;
        _invoiceLineAppService = invoiceLineAppService;
        _paymentAppService = paymentAppService;
        _productAppService = productAppService;
        _taxRateAppService = taxRateAppService;
        _customerRepository = customerRepository;
        _emailSender = emailSender;
        _companyOptions = companyOptions.Value;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public InvoiceDto Invoice { get; set; } = null!;
    public IReadOnlyList<InvoiceLineDto> Lines { get; set; } = Array.Empty<InvoiceLineDto>();
    public IReadOnlyList<PaymentDto> Payments { get; set; } = Array.Empty<PaymentDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public List<SelectListItem> TaxRateOptions { get; set; } = new();
    public Customer Customer { get; set; } = null!;

    [BindProperty]
    public CreateUpdateInvoiceLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    [BindProperty]
    public CreateUpdatePaymentDto NewPayment { get; set; } = new()
    {
        PaymentDate = DateTime.Today
    };

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Invoice", Id);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Invoice = await _invoiceAppService.GetAsync(Id);
        Customer = await _customerRepository.GetAsync(Invoice.CustomerId);

        var lines = await _invoiceLineAppService.GetListAsync(new GetInvoiceLineListInput
        {
            InvoiceId = Id,
            MaxResultCount = 1000
        });
        Lines = lines.Items;

        var payments = await _paymentAppService.GetListAsync(new GetPaymentListInput
        {
            InvoiceId = Id,
            MaxResultCount = 1000
        });
        Payments = payments.Items;

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
        NewLine.InvoiceId = Id;
        await _invoiceLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _invoiceLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddPaymentAsync()
    {
        NewPayment.InvoiceId = Id;
        await _paymentAppService.CreateAsync(NewPayment);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeletePaymentAsync(Guid paymentId)
    {
        await _paymentAppService.DeleteAsync(paymentId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadAsync();
        var pdfBytes = InvoicePdfDocument.Generate(Invoice, Lines, Payments, Customer, _companyOptions);
        return File(pdfBytes, "application/pdf", $"{Invoice.InvoiceNumber}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        await LoadAsync();

        if (!string.IsNullOrWhiteSpace(Customer.Email))
        {
            var pdfBytes = InvoicePdfDocument.Generate(Invoice, Lines, Payments, Customer, _companyOptions);
            await _emailSender.SendAsync(
                Customer.Email,
                $"Invoice {Invoice.InvoiceNumber}",
                $"Dear {Customer.Name},\n\nPlease find attached invoice {Invoice.InvoiceNumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{Invoice.InvoiceNumber}.pdf", File = pdfBytes }
                    }
                }
            );
        }

        return RedirectToPage(new { id = Id });
    }
}
