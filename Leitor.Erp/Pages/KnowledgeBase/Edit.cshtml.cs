using System;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.KnowledgeBase;
using Leitor.Erp.Services.KnowledgeBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.KnowledgeBase;

[Authorize(Policy = ErpPermissions.KnowledgeBase.Edit)]
public class EditModel : AbpPageModel
{
    private readonly KnowledgeArticleAppService _knowledgeArticleAppService;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(KnowledgeArticleAppService knowledgeArticleAppService, IFeatureChecker featureChecker)
    {
        _knowledgeArticleAppService = knowledgeArticleAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateKnowledgeArticleDto Article { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.KnowledgeManagement))
        {
            return NotFound();
        }

        var article = await _knowledgeArticleAppService.GetAsync(Id);
        Article = new CreateUpdateKnowledgeArticleDto
        {
            Title = article.Title,
            Body = article.Body,
            Status = article.Status,
            Tags = article.Tags,
            SourceTicketId = article.SourceTicketId
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _knowledgeArticleAppService.UpdateAsync(Id, Article);
        return RedirectToPage("./Detail", new { id = Id });
    }
}
