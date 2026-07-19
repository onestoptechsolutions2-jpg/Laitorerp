using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Opportunities.Assessments;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class EditModel : AbpPageModel
{
    private readonly NeedsAssessmentAppService _needsAssessmentAppService;
    private readonly NeedsAssessmentAttachmentAppService _needsAssessmentAttachmentAppService;

    public EditModel(
        NeedsAssessmentAppService needsAssessmentAppService,
        NeedsAssessmentAttachmentAppService needsAssessmentAttachmentAppService)
    {
        _needsAssessmentAppService = needsAssessmentAppService;
        _needsAssessmentAttachmentAppService = needsAssessmentAttachmentAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateNeedsAssessmentDto Assessment { get; set; } = new();

    public IReadOnlyList<NeedsAssessmentAttachmentDto> Attachments { get; set; } = Array.Empty<NeedsAssessmentAttachmentDto>();

    public async Task OnGetAsync()
    {
        var assessment = await _needsAssessmentAppService.GetAsync(Id);
        Assessment = new CreateUpdateNeedsAssessmentDto
        {
            OpportunityId = assessment.OpportunityId,
            Type = assessment.Type,
            ConductedDate = assessment.ConductedDate,
            ConductedByUserId = assessment.ConductedByUserId,
            Findings = assessment.Findings,
            Risks = assessment.Risks,
            Recommendations = assessment.Recommendations,
            CustomerRequirements = assessment.CustomerRequirements
        };

        Attachments = await _needsAssessmentAttachmentAppService.GetListAsync(Id);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Assessment.OpportunityId = OpportunityId;

        if (!ModelState.IsValid)
        {
            Attachments = await _needsAssessmentAttachmentAppService.GetListAsync(Id);
            return Page();
        }

        await _needsAssessmentAppService.UpdateAsync(Id, Assessment);
        return RedirectToPage("/Opportunities/Detail", new { id = OpportunityId });
    }

    public async Task<IActionResult> OnPostDeleteAttachmentAsync(Guid attachmentId)
    {
        await _needsAssessmentAttachmentAppService.DeleteAsync(attachmentId);
        return RedirectToPage(new { id = Id, opportunityId = OpportunityId });
    }
}
