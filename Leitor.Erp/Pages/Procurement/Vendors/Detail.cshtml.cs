using System;
using System.Collections.Generic;
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

namespace Leitor.Erp.Pages.Procurement.Vendors;

[Authorize(Policy = ErpPermissions.Vendors.Default)]
public class DetailModel : AbpPageModel
{
    private readonly VendorAppService _vendorAppService;
    private readonly VendorContactAppService _vendorContactAppService;
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly SupplierInvoiceAppService _supplierInvoiceAppService;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;

    public DetailModel(
        VendorAppService vendorAppService,
        VendorContactAppService vendorContactAppService,
        PurchaseOrderAppService purchaseOrderAppService,
        SupplierInvoiceAppService supplierInvoiceAppService,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository)
    {
        _vendorAppService = vendorAppService;
        _vendorContactAppService = vendorContactAppService;
        _purchaseOrderAppService = purchaseOrderAppService;
        _supplierInvoiceAppService = supplierInvoiceAppService;
        _goodsReceiptRepository = goodsReceiptRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public VendorDto Vendor { get; set; } = null!;
    public IReadOnlyList<VendorContactDto> Contacts { get; set; } = Array.Empty<VendorContactDto>();
    public IReadOnlyList<PurchaseOrderDto> PurchaseOrders { get; set; } = Array.Empty<PurchaseOrderDto>();
    public IReadOnlyList<SupplierInvoiceDto> SupplierInvoices { get; set; } = Array.Empty<SupplierInvoiceDto>();

    // Simple vendor performance snapshot: committed spend/order count from Purchase Orders, lead
    // time averaged across every GoodsReceipt against this vendor's POs (OrderDate -> ReceivedDate).
    public decimal TotalSpend { get; set; }
    public int OrderCount { get; set; }
    public double? AverageLeadTimeDays { get; set; }

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Vendors.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Vendor = await _vendorAppService.GetAsync(Id);

        var contacts = await _vendorContactAppService.GetListAsync(new GetVendorContactListInput
        {
            VendorId = Id,
            MaxResultCount = 1000
        });
        Contacts = contacts.Items;

        var purchaseOrders = await _purchaseOrderAppService.GetListAsync(new GetPurchaseOrderListInput
        {
            VendorId = Id,
            MaxResultCount = 1000
        });
        PurchaseOrders = purchaseOrders.Items.OrderByDescending(x => x.OrderDate).ToList();

        var supplierInvoices = await _supplierInvoiceAppService.GetListAsync(new GetSupplierInvoiceListInput
        {
            VendorId = Id,
            MaxResultCount = 1000
        });
        SupplierInvoices = supplierInvoices.Items.OrderByDescending(x => x.IssueDate).ToList();

        TotalSpend = PurchaseOrders.Sum(x => x.Total);
        OrderCount = PurchaseOrders.Count;

        var orderDatesByPoId = PurchaseOrders.ToDictionary(x => x.Id, x => x.OrderDate);
        var purchaseOrderIds = orderDatesByPoId.Keys.ToList();
        var receipts = purchaseOrderIds.Count > 0
            ? await _goodsReceiptRepository.GetListAsync(x => purchaseOrderIds.Contains(x.PurchaseOrderId))
            : new List<GoodsReceipt>();

        var leadTimes = receipts
            .Select(x => (x.ReceivedDate - orderDatesByPoId[x.PurchaseOrderId]).TotalDays)
            .ToList();
        AverageLeadTimeDays = leadTimes.Count > 0 ? leadTimes.Average() : null;
    }

    public async Task<IActionResult> OnPostDeleteContactAsync(Guid contactId)
    {
        await _vendorContactAppService.DeleteAsync(contactId);
        return RedirectToPage(new { id = Id });
    }
}