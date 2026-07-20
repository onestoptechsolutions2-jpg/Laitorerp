using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp;
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
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public CreateModel(
        SupplierInvoiceAppService supplierInvoiceAppService,
        SupplierInvoiceLineAppService supplierInvoiceLineAppService,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _supplierInvoiceAppService = supplierInvoiceAppService;
        _supplierInvoiceLineAppService = supplierInvoiceLineAppService;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid PurchaseOrderId { get; set; }

    public string PONumber { get; set; } = string.Empty;
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

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
        // Defaults to the PO's own currency - the vendor's invoice is expected to bill in the
        // same currency the PO was raised in, but this stays editable for the (rare) mismatch.
        SupplierInvoice.CurrencyCode = purchaseOrder.CurrencyCode;
        await LoadCurrencyOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var purchaseOrder = await _purchaseOrderRepository.GetAsync(PurchaseOrderId);

        if (!ModelState.IsValid)
        {
            PONumber = purchaseOrder.PONumber;
            await LoadCurrencyOptionsAsync();
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

        // Lines are only known once this loop finishes (unlike an Order->Invoice conversion,
        // which builds everything atomically) - post to the ledger now that the total is real.
        // A failure here (e.g. no account configured yet for a required system role) shouldn't
        // undo an otherwise-successful invoice creation - the "Post to Ledger" button on Detail
        // covers that retry case instead of surfacing an error on this page.
        if (invoice.Status == SupplierInvoiceStatus.Issued && poLines.Count > 0)
        {
            try
            {
                await _supplierInvoiceAppService.PostToLedgerAsync(invoice.Id);
            }
            catch (UserFriendlyException)
            {
                // Swallowed deliberately - see comment above.
            }
        }

        return RedirectToPage("./Detail", new { id = invoice.Id });
    }

    private async Task LoadCurrencyOptionsAsync()
    {
        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive);
        CurrencyOptions = currencies
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code, x.Code))
            .ToList();
    }
}
