using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

[Authorize(Policy = ErpPermissions.Procurement.Default)]
public class IndexModel : AbpPageModel
{
    private readonly PurchaseOrderAppService _purchaseOrderAppService;

    public IndexModel(PurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<PurchaseOrderDto> PurchaseOrders { get; set; } = Array.Empty<PurchaseOrderDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _purchaseOrderAppService.GetListAsync(new GetPurchaseOrderListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        PurchaseOrders = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _purchaseOrderAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
