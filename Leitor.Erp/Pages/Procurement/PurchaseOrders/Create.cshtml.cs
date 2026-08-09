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
    private readonly VendorAppService _vendorAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;

    public CreateModel(
        PurchaseOrderAppService purchaseOrderAppService,
        VendorAppService vendorAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<Warehouse, Guid> warehouseRepository)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _vendorAppService = vendorAppService;
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

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostCreateVendorAsync([FromBody] CreateVendorRequest request)
    {
        // AJAX handler for creating a new vendor from this form - same shape as Sales/Quotes/
        // Detail's OnPostCreateProductAsync. A brand-new deployment has zero vendors and this
        // dropdown has no "None" fallback, so without this a first-ever Purchase Order is a dead
        // end: navigate away, create the vendor, come back, start the form over.
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return new JsonResult(new { error = "Vendor name is required" }) { StatusCode = 400 };
            }

            var vendor = await _vendorAppService.CreateAsync(new CreateUpdateVendorDto
            {
                Name = request.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
            });

            return new JsonResult(new { id = vendor.Id, name = vendor.Name, defaultPaymentTerms = (int)vendor.DefaultPaymentTerms });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Error creating vendor: {ex.Message}" }) { StatusCode = 400 };
        }
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
