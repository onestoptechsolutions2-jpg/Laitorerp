using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Opportunities.Proposals;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly ProposalAppService _proposalAppService;

    public CreateModel(ProposalAppService proposalAppService)
    {
        _proposalAppService = proposalAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SupersedesProposalId { get; set; }

    [BindProperty]
    public CreateUpdateProposalDto Proposal { get; set; } = new();

    public void OnGet()
    {
        Proposal.OpportunityId = OpportunityId;
        Proposal.SupersedesProposalId = SupersedesProposalId;
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
        return RedirectToPage("/Opportunities/Detail", new { id = OpportunityId });
    }
}
