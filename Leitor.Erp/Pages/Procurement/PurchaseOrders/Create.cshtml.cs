using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

[Authorize(Policy = ErpPermissions.Procurement.Create)]
public class CreateModel : AbpPageModel
{
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;

    public CreateModel(
        PurchaseOrderAppService purchaseOrderAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<Warehouse, Guid> warehouseRepository)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _vendorRepository = vendorRepository;
        _currencyRepository = currencyRepository;
        _warehouseRepository = warehouseRepository;
    }

    [BindProperty]
    public CreateUpdatePurchaseOrderDto PurchaseOrder { get; set; } = new()
    {
        OrderDate = DateTime.Today
    };

    public List<SelectListItem> VendorOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();
    public List<SelectListItem> WarehouseOptions { get; set; } = new();

    // Keyed by Vendor.Id.ToString(), valued by the (int)PaymentTerms - serializes into the page's
    // inline script so the PaymentTerms field follows the Vendor dropdown client-side, same
    // pattern as Pages/Sales/Quotes/Create.cshtml's Customer -> Currency inheritance.
    public Dictionary<string, int> VendorDefaultPaymentTerms { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadVendorOptionsAsync();
        await LoadCurrencyOptionsAsync();
        await LoadWarehouseOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadVendorOptionsAsync();
            await LoadCurrencyOptionsAsync();
            await LoadWarehouseOptionsAsync();
            return Page();
        }

        var purchaseOrder = await _purchaseOrderAppService.CreateAsync(PurchaseOrder);
        return RedirectToPage("./Detail", new { id = purchaseOrder.Id });
    }

    private async Task LoadWarehouseOptionsAsync()
    {
        var warehouses = await _warehouseRepository.GetListAsync(x => x.IsActive);
        WarehouseOptions = warehouses
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        PurchaseOrder.WarehouseId ??= warehouses.FirstOrDefault(x => x.IsDefault)?.Id;
    }

    private async Task LoadVendorOptionsAsync()
    {
        var vendors = await _vendorRepository.GetListAsync();
        VendorOptions = vendors
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        VendorDefaultPaymentTerms = vendors.ToDictionary(x => x.Id.ToString(), x => (int)x.DefaultPaymentTerms);
    }

    private async Task LoadCurrencyOptionsAsync()
    {
        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive);
        CurrencyOptions = currencies
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code, x.Code))
            .ToList();

        if (string.IsNullOrWhiteSpace(PurchaseOrder.CurrencyCode))
        {
            PurchaseOrder.CurrencyCode = currencies.FirstOrDefault(x => x.IsBaseCurrency)?.Code ?? string.Empty;
        }
    }
}
