using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class IndexModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;

    public IndexModel(OrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Delete);

        var result = await _orderAppService.GetListAsync(new GetOrderListInput
        {
            Filter = Filter,
            MaxResultCount = 1000
        });

        Orders = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _orderAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter });
    }
}
