using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Opportunities;
using Leitor.Erp.Services.Partners;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class DetailModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly NeedsAssessmentAppService _needsAssessmentAppService;
    private readonly ProposalAppService _proposalAppService;
    private readonly CommissionAppService _commissionAppService;
    private readonly QuoteAppService _quoteAppService;
    private readonly QuoteLineAppService _quoteLineAppService;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IFeatureChecker _featureChecker;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;

    public DetailModel(
        OpportunityAppService opportunityAppService,
        NeedsAssessmentAppService needsAssessmentAppService,
        ProposalAppService proposalAppService,
        CommissionAppService commissionAppService,
        QuoteAppService quoteAppService,
        QuoteLineAppService quoteLineAppService,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Customer, Guid> customerRepository,
        IFeatureChecker featureChecker,
        IEmailSender emailSender,
        ErpCompanyProfileProvider companyProfileProvider)
    {
        _opportunityAppService = opportunityAppService;
        _needsAssessmentAppService = needsAssessmentAppService;
        _proposalAppService = proposalAppService;
        _commissionAppService = commissionAppService;
        _quoteAppService = quoteAppService;
        _quoteLineAppService = quoteLineAppService;
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _featureChecker = featureChecker;
        _emailSender = emailSender;
        _companyProfileProvider = companyProfileProvider;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OpportunityDto Opportunity { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public IReadOnlyList<NeedsAssessmentDto> Assessments { get; set; } = Array.Empty<NeedsAssessmentDto>();
    public IReadOnlyList<ProposalDto> Proposals { get; set; } = Array.Empty<ProposalDto>();
    public IReadOnlyList<CommissionDto> Commissions { get; set; } = Array.Empty<CommissionDto>();

    // "Share Package" needs at least one document to attach and a customer email to send it to -
    // gated the same way every other Email action in this app hides itself when Customer.Email is
    // blank, plus this document-existence check specifically.
    public bool CanSharePackage => !string.IsNullOrWhiteSpace(Customer?.Email) && (Assessments.Count > 0 || Proposals.Count > 0 || QuoteIdByProposalId.Count > 0);

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
        Customer = await _customerRepository.GetAsync(Opportunity.CustomerId);

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

    // Combines whichever of Assessment/Proposal/Quote exist for this Opportunity into one email
    // instead of the customer receiving three separate ones - the most recent of each, since a
    // superseded Proposal or an older Assessment isn't what should represent "our current offer."
    public async Task<IActionResult> OnPostSharePackageAsync()
    {
        var opportunity = await _opportunityAppService.GetAsync(Id);
        var customer = await _customerRepository.GetAsync(opportunity.CustomerId);

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            return RedirectToPage(new { id = Id });
        }

        var companyOptions = await _companyProfileProvider.GetAsync();
        var attachments = new List<EmailAttachment>();

        var latestAssessment = (await _needsAssessmentAppService.GetListAsync(new GetNeedsAssessmentListInput { OpportunityId = Id, MaxResultCount = 1000 }))
            .Items.OrderByDescending(x => x.ConductedDate).FirstOrDefault();
        if (latestAssessment != null)
        {
            var pdfBytes = NeedsAssessmentPdfDocument.Generate(latestAssessment, customer, companyOptions);
            attachments.Add(new EmailAttachment { Name = $"Assessment-{latestAssessment.ConductedDate:yyyy-MM-dd}.pdf", File = pdfBytes });
        }

        var latestProposal = (await _proposalAppService.GetListAsync(new GetProposalListInput { OpportunityId = Id, MaxResultCount = 1000 }))
            .Items.Where(x => x.Status != ProposalStatus.Superseded).OrderByDescending(x => x.CreationTime).FirstOrDefault();
        Guid? quoteId = null;
        if (latestProposal != null)
        {
            var pdfBytes = ProposalPdfDocument.Generate(latestProposal, customer, companyOptions);
            attachments.Add(new EmailAttachment { Name = $"{latestProposal.ProposalNumber}.pdf", File = pdfBytes });

            quoteId = (await _quoteRepository.GetListAsync(x => x.ProposalId == latestProposal.Id)).FirstOrDefault()?.Id;
        }

        if (quoteId.HasValue)
        {
            var quote = await _quoteAppService.GetAsync(quoteId.Value);
            var lines = await _quoteLineAppService.GetListAsync(new GetQuoteLineListInput { QuoteId = quoteId.Value, MaxResultCount = 1000 });
            var pdfBytes = QuotePdfDocument.Generate(quote, lines.Items, customer, companyOptions);
            attachments.Add(new EmailAttachment { Name = $"{quote.QuoteNumber}.pdf", File = pdfBytes });
        }

        if (attachments.Count == 0)
        {
            return RedirectToPage(new { id = Id });
        }

        await _emailSender.SendAsync(
            customer.Email,
            $"Your proposal from {companyOptions.Name}",
            $"Hello {customer.Name},\n\nPlease find attached our proposal/quotation based on the assessment carried out. Kindly review and let us know if you have any questions or would like us to proceed.\n\nRegards,\n{companyOptions.Name}",
            isBodyHtml: false,
            new AdditionalEmailSendingArgs { Attachments = attachments }
        );

        return RedirectToPage(new { id = Id });
    }
}
