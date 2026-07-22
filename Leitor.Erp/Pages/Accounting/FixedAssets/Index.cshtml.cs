using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.FixedAssets;

[Authorize(Policy = ErpPermissions.FixedAssets.Default)]
public class IndexModel : AbpPageModel
{
    private readonly FixedAssetAppService _fixedAssetAppService;

    public IndexModel(FixedAssetAppService fixedAssetAppService)
    {
        _fixedAssetAppService = fixedAssetAppService;
    }

    public IReadOnlyList<FixedAssetDto> Assets { get; set; } = Array.Empty<FixedAssetDto>();
    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.FixedAssets.Edit);

        var result = await _fixedAssetAppService.GetListAsync(new GetFixedAssetListInput
        {
            MaxResultCount = 1000,
            Sorting = "AssetNumber"
        });
        Assets = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _fixedAssetAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
