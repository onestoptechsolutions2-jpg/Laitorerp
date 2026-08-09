using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Opportunities;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class DetailModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly NeedsAssessmentAppService _needsAssessmentAppService;
    private readonly ProposalAppService _proposalAppService;
    private readonly CommissionAppService _commissionAppService;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        OpportunityAppService opportunityAppService,
        NeedsAssessmentAppService needsAssessmentAppService,
        ProposalAppService proposalAppService,
        CommissionAppService commissionAppService,
        IRepository<Quote, Guid> quoteRepository,
        IFeatureChecker featureChecker)
    {
        _opportunityAppService = opportunityAppService;
        _needsAssessmentAppService = needsAssessmentAppService;
        _proposalAppService = proposalAppService;
        _commissionAppService = commissionAppService;
        _quoteRepository = quoteRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OpportunityDto Opportunity { get; set; } = null!;
    public IReadOnlyList<NeedsAssessmentDto> Assessments { get; set; } = Array.Empty<NeedsAssessmentDto>();
    public IReadOnlyList<ProposalDto> Proposals { get; set; } = Array.Empty<ProposalDto>();
    public IReadOnlyList<CommissionDto> Commissions { get; set; } = Array.Empty<CommissionDto>();

    // Once a Proposal already has a Quote (or was Rejected/Superseded), attempting the conversion
    // again would just throw - the view hides the button and links to the existing Quote instead.
    public Dictionary<Guid, Guid> QuoteIdByProposalId { get; set; } = new();

    // A Superseded proposal's row offers "Create Replacement" only until a replacement actually
    // exists (SupersedesProposalId pointing back at it) - after that it links to the replacement
    // instead of offering to create a second one.
    public Dictionary<Guid, Guid> ReplacementProposalIdBySupersededId { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool ShowPartnerCommission { get; set; }

    public bool CanConvertProposal(ProposalDto proposal) =>
        !QuoteIdByProposalId.ContainsKey(proposal.Id) && proposal.Status is not (ProposalStatus.Rejected or ProposalStatus.Superseded);

    public bool CanSupersedeProposal(ProposalDto proposal) =>
        proposal.Status is not (ProposalStatus.Rejected or ProposalStatus.Superseded);

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Edit);
        ShowPartnerCommission = await AuthorizationService.IsGrantedAsync(ErpPermissions.Partners.Default) &&
                                 await _featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission);

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

        ReplacementProposalIdBySupersededId = Proposals
            .Where(x => x.SupersedesProposalId.HasValue)
            .ToDictionary(x => x.SupersedesProposalId!.Value, x => x.Id);

        if (ShowPartnerCommission)
        {
            var commissions = await _commissionAppService.GetListAsync(new GetCommissionListInput
            {
                OpportunityId = Id,
                MaxResultCount = 1000
            });
            Commissions = commissions.Items;
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

    public async Task<IActionResult> OnPostSupersedeProposalAsync(Guid proposalId, string supersedeReason)
    {
        await _proposalAppService.SupersedeAsync(proposalId, supersedeReason);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostMarkCommissionPaidAsync(Guid commissionId)
    {
        await _commissionAppService.MarkPaidAsync(commissionId);
        return RedirectToPage(new { id = Id });
    }
}
