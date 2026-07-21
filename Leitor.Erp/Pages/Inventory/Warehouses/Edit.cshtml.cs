using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Inventory.Warehouses;

[Authorize(Policy = ErpPermissions.Inventory.Edit)]
public class EditModel : AbpPageModel
{
    private readonly WarehouseAppService _warehouseAppService;

    public EditModel(WarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateWarehouseDto Warehouse { get; set; } = new();

    public async Task OnGetAsync()
    {
        var warehouse = await _warehouseAppService.GetAsync(Id);
        Warehouse = new CreateUpdateWarehouseDto
        {
            Name = warehouse.Name,
            Address = warehouse.Address,
            IsDefault = warehouse.IsDefault,
            IsActive = warehouse.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _warehouseAppService.UpdateAsync(Id, Warehouse);
        return RedirectToPage("./Index");
    }
}
