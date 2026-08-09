using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Changes;

[Authorize(Policy = ErpPermissions.Changes.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ChangeRequestAppService _changeRequestAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(ChangeRequestAppService changeRequestAppService, IFeatureChecker featureChecker)
    {
        _changeRequestAppService = changeRequestAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<ChangeRequestDto> Changes { get; set; } = Array.Empty<ChangeRequestDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ChangeEnablement))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Changes.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Changes.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _changeRequestAppService.GetListAsync(new GetChangeRequestListInput
        {
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Changes = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _changeRequestAppService.DeleteAsync(id);
        return RedirectToPage(new { PageIndex });
    }
}
