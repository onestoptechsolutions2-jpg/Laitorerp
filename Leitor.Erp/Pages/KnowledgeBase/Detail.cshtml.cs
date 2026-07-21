using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.KnowledgeBase;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.KnowledgeBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.KnowledgeBase;

[Authorize(Policy = ErpPermissions.KnowledgeBase.Default)]
public class DetailModel : AbpPageModel
{
    private readonly KnowledgeArticleAppService _knowledgeArticleAppService;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        KnowledgeArticleAppService knowledgeArticleAppService,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IFeatureChecker featureChecker)
    {
        _knowledgeArticleAppService = knowledgeArticleAppService;
        _deletionRequestRepository = deletionRequestRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public KnowledgeArticleDto Article { get; set; } = null!;

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.KnowledgeManagement))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.KnowledgeBase.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "KnowledgeArticle", Id);
        Article = await _knowledgeArticleAppService.GetAsync(Id);
        return Page();
    }
}
