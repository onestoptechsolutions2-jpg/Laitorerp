using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Assets;

[Authorize(Policy = ErpPermissions.Assets.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ConfigurationItemAppService _configurationItemAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(ConfigurationItemAppService configurationItemAppService, IFeatureChecker featureChecker)
    {
        _configurationItemAppService = configurationItemAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<ConfigurationItemDto> Items { get; set; } = Array.Empty<ConfigurationItemDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.AssetManagement))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Assets.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Assets.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _configurationItemAppService.GetListAsync(new GetConfigurationItemListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Items = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _configurationItemAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
