using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.KnowledgeBase;
using Leitor.Erp.Services.KnowledgeBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.KnowledgeBase;

[Authorize(Policy = ErpPermissions.KnowledgeBase.Default)]
public class IndexModel : AbpPageModel
{
    private readonly KnowledgeArticleAppService _knowledgeArticleAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(KnowledgeArticleAppService knowledgeArticleAppService, IFeatureChecker featureChecker)
    {
        _knowledgeArticleAppService = knowledgeArticleAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<KnowledgeArticleDto> Articles { get; set; } = Array.Empty<KnowledgeArticleDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.KnowledgeManagement))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.KnowledgeBase.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.KnowledgeBase.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _knowledgeArticleAppService.GetListAsync(new GetKnowledgeArticleListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Articles = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _knowledgeArticleAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
