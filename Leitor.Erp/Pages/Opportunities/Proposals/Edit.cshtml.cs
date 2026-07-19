using System;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Opportunities.Proposals;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ProposalAppService _proposalAppService;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly ErpCompanyOptions _companyOptions;

    public EditModel(
        ProposalAppService proposalAppService,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Customer, Guid> customerRepository,
        IOptions<ErpCompanyOptions> companyOptions)
    {
        _proposalAppService = proposalAppService;
        _opportunityRepository = opportunityRepository;
        _customerRepository = customerRepository;
        _companyOptions = companyOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateProposalDto Proposal { get; set; } = new();

    public ProposalDto ProposalDetails { get; set; } = null!;

    public async Task OnGetAsync()
    {
        ProposalDetails = await _proposalAppService.GetAsync(Id);
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

    public async Task<IActionResult> OnGetPdfAsync()
    {
        var proposal = await _proposalAppService.GetAsync(Id);
        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var customer = await _customerRepository.GetAsync(opportunity.CustomerId);

        var pdfBytes = ProposalPdfDocument.Generate(proposal, customer, _companyOptions);
        return File(pdfBytes, "application/pdf", $"{proposal.ProposalNumber}.pdf");
    }
}
