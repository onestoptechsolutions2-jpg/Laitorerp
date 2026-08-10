using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Procurement;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Leitor.Erp.Settings;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class DetailModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;
    private readonly OrderLineAppService _orderLineAppService;
    private readonly ProductAppService _productAppService;
    private readonly TaxRateAppService _taxRateAppService;
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly OrderPaymentMilestoneAppService _milestoneAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<FieldServiceJob, Guid> _fieldServiceJobRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<PriceListItem, Guid> _priceListItemRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;
    private ErpCompanyOptions _companyOptions = null!;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        OrderAppService orderAppService,
        OrderLineAppService orderLineAppService,
        ProductAppService productAppService,
        TaxRateAppService taxRateAppService,
        PurchaseOrderAppService purchaseOrderAppService,
        OrderPaymentMilestoneAppService milestoneAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<FieldServiceJob, Guid> fieldServiceJobRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<PriceListItem, Guid> priceListItemRepository,
        IEmailSender emailSender,
        ErpCompanyProfileProvider companyProfileProvider,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _orderAppService = orderAppService;
        _orderLineAppService = orderLineAppService;
        _productAppService = productAppService;
        _taxRateAppService = taxRateAppService;
        _purchaseOrderAppService = purchaseOrderAppService;
        _milestoneAppService = milestoneAppService;
        _customerRepository = customerRepository;
        _fieldServiceJobRepository = fieldServiceJobRepository;
        _invoiceRepository = invoiceRepository;
        _priceListItemRepository = priceListItemRepository;
        _emailSender = emailSender;
        _companyProfileProvider = companyProfileProvider;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OrderDto Order { get; set; } = null!;
    public IReadOnlyList<OrderLineDto> Lines { get; set; } = Array.Empty<OrderLineDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public List<SelectListItem> TaxRateOptions { get; set; } = new();
    public Customer Customer { get; set; } = null!;
    public IReadOnlyList<PurchaseOrderDto> PurchaseOrders { get; set; } = Array.Empty<PurchaseOrderDto>();
    public IReadOnlyList<OrderPaymentMilestoneDto> Milestones { get; set; } = Array.Empty<OrderPaymentMilestoneDto>();
    public IReadOnlyList<FieldServiceJob> Jobs { get; set; } = Array.Empty<FieldServiceJob>();
    public bool CanIssueFinalInvoice { get; set; }
    public OrderWizardStep CurrentStep { get; set; }

    [BindProperty]
    public CreateUpdateOrderLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    [BindProperty]
    public CreateUpdateOrderPaymentMilestoneDto NewMilestone { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Order", Id);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Order = await _orderAppService.GetAsync(Id);
        Customer = await _customerRepository.GetAsync(Order.CustomerId);

        var lines = await _orderLineAppService.GetListAsync(new GetOrderLineListInput
        {
            OrderId = Id,
            MaxResultCount = 1000
        });
        Lines = lines.Items;

        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            IsActive = true,
            MaxResultCount = 1000
        });

        // Suggests the customer's price-list price where one exists, rather than the product's
        // standard price - purely a label/starting-point change, UnitPrice stays a plain editable
        // field on the Add Line form either way.
        var priceOverridesByProductId = Customer.DefaultPriceListId.HasValue
            ? (await _priceListItemRepository.GetListAsync(x => x.PriceListId == Customer.DefaultPriceListId.Value))
                .ToDictionary(x => x.ProductId, x => x.UnitPrice)
            : new Dictionary<Guid, decimal>();

        ProductOptions = new List<SelectListItem> { new(L["None"], "") };
        ProductOptions.AddRange(
            products.Items.OrderBy(x => x.Name).Select(x => new SelectListItem(
                $"{x.Name} ({priceOverridesByProductId.GetValueOrDefault(x.Id, x.UnitPrice):N2})",
                x.Id.ToString()))
        );

        var taxRates = await _taxRateAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        TaxRateOptions = new List<SelectListItem> { new(L["UseDefaultTaxRate"], "") };
        TaxRateOptions.AddRange(
            taxRates.Items.OrderBy(x => x.Name).Select(x => new SelectListItem($"{x.Name} ({x.Percent:N0}%)", x.Id.ToString()))
        );

        var purchaseOrders = await _purchaseOrderAppService.GetListAsync(new GetPurchaseOrderListInput
        {
            SourceOrderId = Id,
            MaxResultCount = 1000
        });
        PurchaseOrders = purchaseOrders.Items;

        Milestones = await _milestoneAppService.GetListAsync(Id);

        Jobs = (await _fieldServiceJobRepository.GetListAsync(x => x.OrderId == Id))
            .OrderBy(x => x.ScheduledDate)
            .ToList();

        var hasFinalInvoice = Order.PaymentTerms == PaymentTerms.Milestone
            ? Milestones.Any(x => x.Kind == OrderPaymentMilestoneKind.Final && x.IsInvoiced)
            : (await _invoiceRepository.GetListAsync(x => x.OrderId == Id)).Any();

        // Mirrors the guard OrderAppService.ConvertToInvoiceAsync/ConvertMilestoneToInvoiceAsync
        // both enforce - hides the button rather than letting the click surface a raw
        // UserFriendlyException as an unstyled ABP error page (OnPostIssueFinalInvoiceAsync's
        // try/catch below is the remaining safety net, e.g. a job gets un-completed between page
        // load and submit).
        var allJobsCompleted = !Jobs.Any(x => x.Status != FieldServiceJobStatus.Completed);

        CanIssueFinalInvoice = Order.Status is OrderStatus.Confirmed or OrderStatus.Fulfilled && !hasFinalInvoice && allJobsCompleted;

        // Presentational only (see OrderWizardStep's own comment) - Items while the order has no
        // lines yet, Payment once lines exist and the order is still awaiting confirmation (that's
        // where the Confirm button, which locks in payment terms, lives), Review once confirmed.
        CurrentStep = Lines.Count == 0
            ? OrderWizardStep.Items
            : Order.Status == OrderStatus.Submitted
                ? OrderWizardStep.Payment
                : OrderWizardStep.Review;
    }

    public async Task<IActionResult> OnPostAddLineAsync()
    {
        NewLine.OrderId = Id;
        await _orderLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _orderLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostCreateProductAsync([FromBody] CreateProductRequest request)
    {
        // AJAX handler for creating a new product from the order line-add form - same shape as
        // Sales/Quotes/Detail's OnPostCreateProductAsync (the original of this pattern).
        try
        {
            if (request == null)
            {
                return new JsonResult(new { error = "Request body could not be parsed. Ensure Content-Type is application/json" }) { StatusCode = 400 };
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new JsonResult(new { error = "Product name is required" }) { StatusCode = 400 };
            }

            if (request.UnitPrice <= 0)
            {
                return new JsonResult(new { error = "Unit price must be greater than 0" }) { StatusCode = 400 };
            }

            var createDto = new CreateUpdateProductDto
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                UnitPrice = request.UnitPrice,
                Cost = request.Cost,
                TaxRateId = string.IsNullOrEmpty(request.TaxRateId) ? null : Guid.Parse(request.TaxRateId),
                IsActive = true
            };

            var product = await _productAppService.CreateAsync(createDto);

            return new JsonResult(new
            {
                id = product.Id,
                name = product.Name,
                unitPrice = product.UnitPrice
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Error creating product: {ex.Message}" }) { StatusCode = 400 };
        }
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        try
        {
            await _orderAppService.ConfirmAsync(Id);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostIssueFinalInvoiceAsync()
    {
        try
        {
            var invoice = await _orderAppService.IssueFinalInvoiceAsync(Id);
            return RedirectToPage("/Sales/Invoices/Detail", new { id = invoice.Id });
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(new { id = Id });
        }
    }

    public async Task<IActionResult> OnPostAddMilestoneAsync()
    {
        NewMilestone.OrderId = Id;
        await _milestoneAppService.CreateAsync(NewMilestone);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteMilestoneAsync(Guid milestoneId)
    {
        await _milestoneAppService.DeleteAsync(milestoneId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostInvoiceMilestoneAsync(Guid milestoneId)
    {
        try
        {
            var invoice = await _orderAppService.ConvertMilestoneToInvoiceAsync(Id, milestoneId);
            return RedirectToPage("/Sales/Invoices/Detail", new { id = invoice.Id });
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(new { id = Id });
        }
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadAsync();
        _companyOptions = await _companyProfileProvider.GetAsync();
        var pdfBytes = OrderPdfDocument.Generate(Order, Lines, Customer, _companyOptions);
        return File(pdfBytes, "application/pdf", $"{Order.OrderNumber}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        await LoadAsync();
        _companyOptions = await _companyProfileProvider.GetAsync();

        if (!string.IsNullOrWhiteSpace(Customer.Email))
        {
            var pdfBytes = OrderPdfDocument.Generate(Order, Lines, Customer, _companyOptions);
            await _emailSender.SendAsync(
                Customer.Email,
                $"Order {Order.OrderNumber}",
                $"Dear {Customer.Name},\n\nPlease find attached order {Order.OrderNumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{Order.OrderNumber}.pdf", File = pdfBytes }
                    }
                }
            );
        }

        return RedirectToPage(new { id = Id });
    }
}
