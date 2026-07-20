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
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

[Authorize(Policy = ErpPermissions.Procurement.Create)]
public class CreateModel : AbpPageModel
{
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public CreateModel(
        PurchaseOrderAppService purchaseOrderAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _vendorRepository = vendorRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty]
    public CreateUpdatePurchaseOrderDto PurchaseOrder { get; set; } = new()
    {
        OrderDate = DateTime.Today
    };

    public List<SelectListItem> VendorOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadVendorOptionsAsync();
        await LoadCurrencyOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadVendorOptionsAsync();
            await LoadCurrencyOptionsAsync();
            return Page();
        }

        var purchaseOrder = await _purchaseOrderAppService.CreateAsync(PurchaseOrder);
        return RedirectToPage("./Detail", new { id = purchaseOrder.Id });
    }

    private async Task LoadVendorOptionsAsync()
    {
        var vendors = await _vendorRepository.GetListAsync();
        VendorOptions = vendors
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
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
