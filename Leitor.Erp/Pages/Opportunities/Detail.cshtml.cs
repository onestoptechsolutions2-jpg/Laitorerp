using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class DetailModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly NeedsAssessmentAppService _needsAssessmentAppService;
    private readonly ProposalAppService _proposalAppService;
    private readonly IRepository<Quote, Guid> _quoteRepository;

    public DetailModel(
        OpportunityAppService opportunityAppService,
        NeedsAssessmentAppService needsAssessmentAppService,
        ProposalAppService proposalAppService,
        IRepository<Quote, Guid> quoteRepository)
    {
        _opportunityAppService = opportunityAppService;
        _needsAssessmentAppService = needsAssessmentAppService;
        _proposalAppService = proposalAppService;
        _quoteRepository = quoteRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OpportunityDto Opportunity { get; set; } = null!;
    public IReadOnlyList<NeedsAssessmentDto> Assessments { get; set; } = Array.Empty<NeedsAssessmentDto>();
    public IReadOnlyList<ProposalDto> Proposals { get; set; } = Array.Empty<ProposalDto>();

    // Once a Proposal already has a Quote (or was Rejected), attempting the conversion again would
    // just throw - the view hides the button and links to the existing Quote instead.
    public Dictionary<Guid, Guid> QuoteIdByProposalId { get; set; } = new();

    public bool CanEdit { get; set; }

    public bool CanConvertProposal(ProposalDto proposal) =>
        !QuoteIdByProposalId.ContainsKey(proposal.Id) && proposal.Status != ProposalStatus.Rejected;

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

        var proposalIds = Proposals.Select(x => x.Id).ToList();
        if (proposalIds.Count > 0)
        {
            var quotes = await _quoteRepository.GetListAsync(x => x.ProposalId.HasValue && proposalIds.Contains(x.ProposalId.Value));
            QuoteIdByProposalId = quotes.GroupBy(x => x.ProposalId!.Value).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreationTime).First().Id);
        }
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
        // Defends against a double-click/back-button resubmit/second tab hitting this after the
        // button should have been hidden - redirect to the existing Quote instead of letting
        // ConvertToQuoteAsync's guard throw into a raw error page.
        var existingQuote = (await _quoteRepository.GetListAsync(x => x.ProposalId == proposalId)).FirstOrDefault();
        if (existingQuote != null)
        {
            return RedirectToPage("/Sales/Quotes/Detail", new { id = existingQuote.Id });
        }

        var quote = await _proposalAppService.ConvertToQuoteAsync(proposalId);
        return RedirectToPage("/Sales/Quotes/Detail", new { id = quote.Id });
    }
}
