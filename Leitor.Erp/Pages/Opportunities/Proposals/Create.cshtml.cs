using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Opportunities.Proposals;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly ProposalAppService _proposalAppService;
    private readonly IRepository<NeedsAssessment, Guid> _needsAssessmentRepository;

    public CreateModel(ProposalAppService proposalAppService, IRepository<NeedsAssessment, Guid> needsAssessmentRepository)
    {
        _proposalAppService = proposalAppService;
        _needsAssessmentRepository = needsAssessmentRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SupersedesProposalId { get; set; }

    [BindProperty]
    public CreateUpdateProposalDto Proposal { get; set; } = new();

    // True when the form below was pre-filled from an existing assessment, so the page can tell
    // the user where the starter text came from - still fully editable, never locked.
    public bool PrefilledFromAssessment { get; set; }

    public async Task OnGetAsync()
    {
        Proposal.OpportunityId = OpportunityId;
        Proposal.SupersedesProposalId = SupersedesProposalId;

        // Consolidation audit (2026-08-18) flagged that a Proposal was always started from a blank
        // page even when an assessment already exists for this Opportunity, forcing the assessor's
        // findings to be re-typed by hand. Pre-fills starter text from the most recent assessment
        // instead - the fields stay fully editable, this only saves the first draft.
        var assessment = (await _needsAssessmentRepository.GetListAsync(x => x.OpportunityId == OpportunityId))
            .OrderByDescending(x => x.ConductedDate)
            .FirstOrDefault();

        if (assessment != null)
        {
            PrefilledFromAssessment = true;
            Proposal.Summary = assessment.Findings;
            Proposal.ProposedSolution = assessment.Recommendations;
            Proposal.Scope = assessment.CustomerRequirements;
            Proposal.Assumptions = assessment.Risks;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Proposal.OpportunityId = OpportunityId;
        Proposal.SupersedesProposalId = SupersedesProposalId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _proposalAppService.CreateAsync(Proposal);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("/Opportunities/Detail", new { id = OpportunityId }) });
        }

        return RedirectToPage("/Opportunities/Detail", new { id = OpportunityId });
    }
}
