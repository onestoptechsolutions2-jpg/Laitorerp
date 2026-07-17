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
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

[Authorize(Policy = ErpPermissions.Procurement.Create)]
public class CreateModel : AbpPageModel
{
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public CreateModel(
        PurchaseOrderAppService purchaseOrderAppService,
        IRepository<Vendor, Guid> vendorRepository)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _vendorRepository = vendorRepository;
    }

    [BindProperty]
    public CreateUpdatePurchaseOrderDto PurchaseOrder { get; set; } = new()
    {
        OrderDate = DateTime.Today
    };

    public List<SelectListItem> VendorOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadVendorOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadVendorOptionsAsync();
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
}
