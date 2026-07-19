using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Opportunities.Assessments;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly NeedsAssessmentAppService _needsAssessmentAppService;

    public CreateModel(NeedsAssessmentAppService needsAssessmentAppService)
    {
        _needsAssessmentAppService = needsAssessmentAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateNeedsAssessmentDto Assessment { get; set; } = new()
    {
        ConductedDate = DateTime.Today
    };

    public void OnGet()
    {
        Assessment.OpportunityId = OpportunityId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Assessment.OpportunityId = OpportunityId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _needsAssessmentAppService.CreateAsync(Assessment);
        return RedirectToPage("/Opportunities/Detail", new { id = OpportunityId });
    }
}
