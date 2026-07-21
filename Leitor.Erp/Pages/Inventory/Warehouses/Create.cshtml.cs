using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Inventory.Warehouses;

[Authorize(Policy = ErpPermissions.Inventory.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly WarehouseAppService _warehouseAppService;

    public CreateModel(WarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    [BindProperty]
    public CreateUpdateWarehouseDto Warehouse { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _warehouseAppService.CreateAsync(Warehouse);
        return RedirectToPage("./Index");
    }
}
