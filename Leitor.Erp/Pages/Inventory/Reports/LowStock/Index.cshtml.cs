using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Inventory.Reports.LowStock;

[Authorize(Policy = ErpPermissions.Inventory.Default)]
public class IndexModel : AbpPageModel
{
    private readonly InventoryReportAppService _inventoryReportAppService;

    public IndexModel(InventoryReportAppService inventoryReportAppService)
    {
        _inventoryReportAppService = inventoryReportAppService;
    }

    public IReadOnlyList<StockOnHandLineDto> Lines { get; set; } = Array.Empty<StockOnHandLineDto>();

    public async Task OnGetAsync()
    {
        Lines = await _inventoryReportAppService.GetLowStockAsync();
    }
}
