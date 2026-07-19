using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class DetailModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly NeedsAssessmentAppService _needsAssessmentAppService;
    private readonly ProposalAppService _proposalAppService;

    public DetailModel(
        OpportunityAppService opportunityAppService,
        NeedsAssessmentAppService needsAssessmentAppService,
        ProposalAppService proposalAppService)
    {
        _opportunityAppService = opportunityAppService;
        _needsAssessmentAppService = needsAssessmentAppService;
        _proposalAppService = proposalAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OpportunityDto Opportunity { get; set; } = null!;
    public IReadOnlyList<NeedsAssessmentDto> Assessments { get; set; } = Array.Empty<NeedsAssessmentDto>();
    public IReadOnlyList<ProposalDto> Proposals { get; set; } = Array.Empty<ProposalDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Edit);

        Opportunity = await _opportunityAppService.GetAsync(Id);

        var assessments = await _needsAssessmentAppService.GetListAsync(new GetNeedsAssessmentListInput
        {
            OpportunityId = Id,
            MaxResultCount = 1000
        });
        Assessments = assessments.Items;

        var proposals = await _proposalAppService.GetListAsync(new GetProposalListInput
        {
            OpportunityId = Id,
            MaxResultCount = 1000
        });
        Proposals = proposals.Items;
    }

    public async Task<IActionResult> OnPostDeleteAssessmentAsync(Guid assessmentId)
    {
        await _needsAssessmentAppService.DeleteAsync(assessmentId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteProposalAsync(Guid proposalId)
    {
        await _proposalAppService.DeleteAsync(proposalId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostConvertToQuoteAsync(Guid proposalId)
    {
        var quote = await _proposalAppService.ConvertToQuoteAsync(proposalId);
        return RedirectToPage("/Sales/Quotes/Detail", new { id = quote.Id });
    }
}
