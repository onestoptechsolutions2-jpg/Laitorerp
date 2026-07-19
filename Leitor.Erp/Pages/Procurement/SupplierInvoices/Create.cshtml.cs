using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Procurement.SupplierInvoices;

// Prefilled from the source PurchaseOrder's lines rather than a guarded conversion (unlike
// ProposalAppService.ConvertToQuoteAsync etc.) - a vendor's own invoice isn't something this
// system generates, so it's editable from the start: the PO's lines are just a starting point
// staff can adjust for price changes or partial billing before saving.
[Authorize(Policy = ErpPermissions.Procurement.Create)]
public class CreateModel : AbpPageModel
{
    private readonly SupplierInvoiceAppService _supplierInvoiceAppService;
    private readonly SupplierInvoiceLineAppService _supplierInvoiceLineAppService;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;

    public CreateModel(
        SupplierInvoiceAppService supplierInvoiceAppService,
        SupplierInvoiceLineAppService supplierInvoiceLineAppService,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository)
    {
        _supplierInvoiceAppService = supplierInvoiceAppService;
        _supplierInvoiceLineAppService = supplierInvoiceLineAppService;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid PurchaseOrderId { get; set; }

    public string PONumber { get; set; } = string.Empty;

    [BindProperty]
    public CreateUpdateSupplierInvoiceDto SupplierInvoice { get; set; } = new()
    {
        IssueDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30)
    };

    public async Task<IActionResult> OnGetAsync()
    {
        var purchaseOrder = await _purchaseOrderRepository.GetAsync(PurchaseOrderId);
        PONumber = purchaseOrder.PONumber;
        SupplierInvoice.PurchaseOrderId = purchaseOrder.Id;
        SupplierInvoice.VendorId = purchaseOrder.VendorId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var purchaseOrder = await _purchaseOrderRepository.GetAsync(PurchaseOrderId);

        if (!ModelState.IsValid)
        {
            PONumber = purchaseOrder.PONumber;
            return Page();
        }

        SupplierInvoice.PurchaseOrderId = PurchaseOrderId;
        SupplierInvoice.VendorId = purchaseOrder.VendorId;
        var invoice = await _supplierInvoiceAppService.CreateAsync(SupplierInvoice);

        var poLines = await _purchaseOrderLineRepository.GetListAsync(x => x.PurchaseOrderId == PurchaseOrderId);
        foreach (var poLine in poLines)
        {
            await _supplierInvoiceLineAppService.CreateAsync(new CreateUpdateSupplierInvoiceLineDto
            {
                SupplierInvoiceId = invoice.Id,
                ProductId = poLine.ProductId,
                Description = poLine.Description,
                UnitPrice = poLine.UnitPrice,
                Quantity = poLine.Quantity,
                DiscountPercent = poLine.DiscountPercent
            });
        }

        return RedirectToPage("./Detail", new { id = invoice.Id });
    }
}
