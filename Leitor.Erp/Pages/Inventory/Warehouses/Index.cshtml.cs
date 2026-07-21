using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Inventory.Warehouses;

[Authorize(Policy = ErpPermissions.Inventory.Default)]
public class IndexModel : AbpPageModel
{
    private readonly WarehouseAppService _warehouseAppService;

    public IndexModel(WarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public IReadOnlyList<WarehouseDto> Warehouses { get; set; } = Array.Empty<WarehouseDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Inventory.Edit);

        var result = await _warehouseAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 1000
        });
        Warehouses = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _warehouseAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
