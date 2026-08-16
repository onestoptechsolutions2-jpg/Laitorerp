using System;
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

[Authorize(Policy = ErpPermissions.KnowledgeBase.Create)]
public class CreateModel : AbpPageModel
{
    private readonly KnowledgeArticleAppService _knowledgeArticleAppService;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(KnowledgeArticleAppService knowledgeArticleAppService, IFeatureChecker featureChecker)
    {
        _knowledgeArticleAppService = knowledgeArticleAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateKnowledgeArticleDto Article { get; set; } = new();

    // Prefilled when arriving from Ticket Detail's "Promote to KB" action.
    [BindProperty(SupportsGet = true)]
    public Guid? SourceTicketId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SourceTitle { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.KnowledgeManagement))
        {
            return NotFound();
        }

        if (SourceTicketId.HasValue)
        {
            Article.SourceTicketId = SourceTicketId;
            Article.Title = SourceTitle ?? string.Empty;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var article = await _knowledgeArticleAppService.CreateAsync(Article);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = article.Id }) });
        }

        return RedirectToPage("./Detail", new { id = article.Id });
    }
}
