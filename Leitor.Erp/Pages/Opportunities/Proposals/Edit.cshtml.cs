using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.Opportunities.Proposals;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ProposalAppService _proposalAppService;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyOptions _companyOptions;

    public EditModel(
        ProposalAppService proposalAppService,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Customer, Guid> customerRepository,
        IEmailSender emailSender,
        IOptions<ErpCompanyOptions> companyOptions)
    {
        _proposalAppService = proposalAppService;
        _opportunityRepository = opportunityRepository;
        _customerRepository = customerRepository;
        _emailSender = emailSender;
        _companyOptions = companyOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateProposalDto Proposal { get; set; } = new();

    [BindProperty]
    public string UnlockReason { get; set; } = string.Empty;

    public ProposalDto ProposalDetails { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public IReadOnlyList<WorkflowStageEvent> DeliveryHistory { get; set; } = Array.Empty<WorkflowStageEvent>();
    public bool CanUnlock { get; set; }

    public async Task OnGetAsync()
    {
        CanUnlock = await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Unlock);
        ProposalDetails = await _proposalAppService.GetAsync(Id);
        var opportunity = await _opportunityRepository.GetAsync(ProposalDetails.OpportunityId);
        Customer = await _customerRepository.GetAsync(opportunity.CustomerId);
        var history = await _proposalAppService.GetDeliveryHistoryAsync(Id);
        DeliveryHistory = history.Where(x => x.Stage == WorkflowStage.ProposalSent).ToList();
        Proposal = new CreateUpdateProposalDto
        {
            OpportunityId = ProposalDetails.OpportunityId,
            Title = ProposalDetails.Title,
            Status = ProposalDetails.Status,
            Summary = ProposalDetails.Summary,
            ProposedSolution = ProposalDetails.ProposedSolution,
            Scope = ProposalDetails.Scope,
            Timeline = ProposalDetails.Timeline,
            Assumptions = ProposalDetails.Assumptions,
            Exclusions = ProposalDetails.Exclusions,
            WarrantyAndSupport = ProposalDetails.WarrantyAndSupport,
            Terms = ProposalDetails.Terms
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Proposal.OpportunityId = OpportunityId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _proposalAppService.UpdateAsync(Id, Proposal);
        return RedirectToPage("/Opportunities/Detail", new { id = OpportunityId });
    }

    public async Task<IActionResult> OnPostUnlockAsync()
    {
        await _proposalAppService.UnlockForRevisionAsync(Id, UnlockReason);
        return RedirectToPage(new { id = Id, opportunityId = OpportunityId });
    }

    public async Task<IActionResult> OnPostConvertToQuoteAsync()
    {
        var quote = await _proposalAppService.ConvertToQuoteAsync(Id);
        return RedirectToPage("/Sales/Quotes/Detail", new { id = quote.Id });
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        var proposal = await _proposalAppService.GetAsync(Id);
        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var customer = await _customerRepository.GetAsync(opportunity.CustomerId);

        var pdfBytes = ProposalPdfDocument.Generate(proposal, customer, _companyOptions);
        return File(pdfBytes, "application/pdf", $"{proposal.ProposalNumber}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        var proposal = await _proposalAppService.GetAsync(Id);
        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var customer = await _customerRepository.GetAsync(opportunity.CustomerId);

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            var pdfBytes = ProposalPdfDocument.Generate(proposal, customer, _companyOptions);
            await _emailSender.SendAsync(
                customer.Email,
                $"Proposal {proposal.ProposalNumber}",
                $"Dear {customer.Name},\n\nPlease find attached proposal {proposal.ProposalNumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{proposal.ProposalNumber}.pdf", File = pdfBytes }
                    }
                }
            );

            await _proposalAppService.MarkSentAsync(Id, "Email");
        }

        return RedirectToPage(new { id = Id, opportunityId = OpportunityId });
    }

    // Log-only: no WhatsApp API integration (needs a Business API account/credentials the user
    // would have to set up first) - staff send the PDF manually via their own WhatsApp and click
    // this to record who/when for the audit trail.
    public async Task<IActionResult> OnPostMarkSentWhatsAppAsync()
    {
        await _proposalAppService.MarkSentAsync(Id, "WhatsApp");
        return RedirectToPage(new { id = Id, opportunityId = OpportunityId });
    }
}
