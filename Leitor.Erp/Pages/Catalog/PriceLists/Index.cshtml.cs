using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.PriceLists;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class IndexModel : AbpPageModel
{
    private readonly PriceListAppService _priceListAppService;

    public IndexModel(PriceListAppService priceListAppService)
    {
        _priceListAppService = priceListAppService;
    }

    public IReadOnlyList<PriceListDto> PriceLists { get; set; } = Array.Empty<PriceListDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);

        var result = await _priceListAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 1000
        });
        PriceLists = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _priceListAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
