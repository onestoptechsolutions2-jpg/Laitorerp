using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Documents;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Documents;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Procurement;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Leitor.Erp.Settings;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

[Authorize(Policy = ErpPermissions.Procurement.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly PurchaseOrderLineAppService _purchaseOrderLineAppService;
    private readonly ProductAppService _productAppService;
    private readonly GoodsReceiptAppService _goodsReceiptAppService;
    private readonly SupplierInvoiceAppService _supplierInvoiceAppService;
    private readonly TaxRateAppService _taxRateAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<GoodsReceiptLine, Guid> _goodsReceiptLineRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;
    private ErpCompanyOptions _companyOptions = null!;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly DocumentShareLinkService _documentShareLinkService;

    public DetailModel(
        PurchaseOrderAppService purchaseOrderAppService,
        PurchaseOrderLineAppService purchaseOrderLineAppService,
        ProductAppService productAppService,
        GoodsReceiptAppService goodsReceiptAppService,
        SupplierInvoiceAppService supplierInvoiceAppService,
        TaxRateAppService taxRateAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<GoodsReceiptLine, Guid> goodsReceiptLineRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IEmailSender emailSender,
        ErpCompanyProfileProvider companyProfileProvider,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        DocumentShareLinkService documentShareLinkService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _purchaseOrderLineAppService = purchaseOrderLineAppService;
        _productAppService = productAppService;
        _goodsReceiptAppService = goodsReceiptAppService;
        _supplierInvoiceAppService = supplierInvoiceAppService;
        _taxRateAppService = taxRateAppService;
        _vendorRepository = vendorRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _goodsReceiptLineRepository = goodsReceiptLineRepository;
        _warehouseRepository = warehouseRepository;
        _emailSender = emailSender;
        _companyProfileProvider = companyProfileProvider;
        _deletionRequestRepository = deletionRequestRepository;
        _documentShareLinkService = documentShareLinkService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PurchaseOrderDto PurchaseOrder { get; set; } = null!;
    public IReadOnlyList<PurchaseOrderLineDto> Lines { get; set; } = Array.Empty<PurchaseOrderLineDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public Vendor Vendor { get; set; } = null!;
    public Customer? ShipToCustomer { get; set; }
    public string? WhatsAppUrl { get; set; }

    [BindProperty]
    public CreateUpdatePurchaseOrderLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    public IReadOnlyList<GoodsReceiptDto> Receipts { get; set; } = Array.Empty<GoodsReceiptDto>();
    public IReadOnlyList<SupplierInvoiceDto> SupplierInvoices { get; set; } = Array.Empty<SupplierInvoiceDto>();
    public List<SelectListItem> WarehouseOptions { get; set; } = new();
    public List<SelectListItem> TaxRateOptions { get; set; } = new();

    [BindProperty]
    public CreateGoodsReceiptDto NewReceipt { get; set; } = new()
    {
        ReceivedDate = DateTime.Today
    };

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "PurchaseOrder", Id);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        PurchaseOrder = await _purchaseOrderAppService.GetAsync(Id);
        Vendor = await _vendorRepository.GetAsync(PurchaseOrder.VendorId);

        var shareUrl = await _documentShareLinkService.GetOrCreateUrlAsync(DocumentShareType.PurchaseOrder, Id);
        WhatsAppUrl = PhoneLinks.WhatsApp(Vendor.Phone,
            $"Hello {PhoneLinks.FirstName(Vendor.Name)}, please find purchase order {PurchaseOrder.PONumber} here: {shareUrl}. Kindly confirm receipt and expected delivery date.");

        ShipToCustomer = null;
        if (PurchaseOrder.ShipToCustomer && PurchaseOrder.SourceOrderId.HasValue)
        {
            var sourceOrder = await _orderRepository.GetAsync(PurchaseOrder.SourceOrderId.Value);
            ShipToCustomer = await _customerRepository.GetAsync(sourceOrder.CustomerId);
        }

        var lines = await _purchaseOrderLineAppService.GetListAsync(new GetPurchaseOrderLineListInput
        {
            PurchaseOrderId = Id,
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

        var receipts = await _goodsReceiptAppService.GetListAsync(new GetGoodsReceiptListInput { PurchaseOrderId = Id });
        Receipts = receipts;

        var supplierInvoices = await _supplierInvoiceAppService.GetListAsync(new GetSupplierInvoiceListInput
        {
            PurchaseOrderId = Id,
            MaxResultCount = 1000
        });
        SupplierInvoices = supplierInvoices.Items;

        var lineIds = Lines.Select(x => x.Id).ToList();
        var receivedByLineId = lineIds.Count > 0
            ? (await _goodsReceiptLineRepository.GetListAsync(x => lineIds.Contains(x.PurchaseOrderLineId)))
                .GroupBy(x => x.PurchaseOrderLineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityReceived))
            : new Dictionary<Guid, decimal>();

        var warehouses = await _warehouseRepository.GetListAsync(x => x.IsActive);
        WarehouseOptions = warehouses.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        var taxRates = await _taxRateAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        TaxRateOptions = new List<SelectListItem> { new(L["UseDefaultTaxRate"], "") };
        TaxRateOptions.AddRange(
            taxRates.Items.OrderBy(x => x.Name).Select(x => new SelectListItem($"{x.Name} ({x.Percent:N0}%)", x.Id.ToString()))
        );

        NewReceipt.PurchaseOrderId = Id;
        NewReceipt.WarehouseId ??= PurchaseOrder.WarehouseId != Guid.Empty
            ? PurchaseOrder.WarehouseId
            : warehouses.FirstOrDefault(x => x.IsDefault)?.Id;
        NewReceipt.Lines = Lines
            .Select(line => new CreateGoodsReceiptLineDto
            {
                PurchaseOrderLineId = line.Id,
                QuantityReceived = Math.Max(0, line.Quantity - receivedByLineId.GetValueOrDefault(line.Id))
            })
            .ToList();
    }

    public async Task<IActionResult> OnPostAddLineAsync()
    {
        NewLine.PurchaseOrderId = Id;
        await _purchaseOrderLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _purchaseOrderLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRecordReceiptAsync()
    {
        NewReceipt.PurchaseOrderId = Id;
        await _goodsReceiptAppService.CreateAsync(NewReceipt);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadAsync();
        _companyOptions = await _companyProfileProvider.GetAsync();
        var pdfBytes = PurchaseOrderPdfDocument.Generate(PurchaseOrder, Lines, Vendor, _companyOptions, ShipToCustomer);
        return File(pdfBytes, "application/pdf", $"{PurchaseOrder.PONumber}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        await LoadAsync();
        _companyOptions = await _companyProfileProvider.GetAsync();

        if (!string.IsNullOrWhiteSpace(Vendor.Email))
        {
            var shareUrl = await _documentShareLinkService.GetOrCreateUrlAsync(DocumentShareType.PurchaseOrder, Id);
            var pdfBytes = PurchaseOrderPdfDocument.Generate(PurchaseOrder, Lines, Vendor, _companyOptions, ShipToCustomer);
            await _emailSender.SendAsync(
                Vendor.Email,
                $"Purchase Order {PurchaseOrder.PONumber}",
                $"Dear {PhoneLinks.FirstName(Vendor.Name)},\n\nPlease find attached purchase order {PurchaseOrder.PONumber}. You can also view/download it anytime here: {shareUrl}\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{PurchaseOrder.PONumber}.pdf", File = pdfBytes }
                    }
                }
            );
        }

        return RedirectToPage(new { id = Id });
    }
}
