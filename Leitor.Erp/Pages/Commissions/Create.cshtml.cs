using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Commissions;

[Authorize(Policy = ErpPermissions.Partners.Create)]
public class CreateModel : AbpPageModel
{
    private readonly CommissionAppService _commissionAppService;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Partner, Guid> _partnerRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        CommissionAppService commissionAppService,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Partner, Guid> partnerRepository,
        IRepository<Agent, Guid> agentRepository,
        IFeatureChecker featureChecker)
    {
        _commissionAppService = commissionAppService;
        _opportunityRepository = opportunityRepository;
        _partnerRepository = partnerRepository;
        _agentRepository = agentRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateCommissionDto Commission { get; set; } = new();

    public List<SelectListItem> OpportunityOptions { get; set; } = new();
    public List<SelectListItem> PartnerOptions { get; set; } = new();
    public List<SelectListItem> AgentOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission))
        {
            return NotFound();
        }

        if (OpportunityId.HasValue)
        {
            Commission.OpportunityId = OpportunityId.Value;

            // Pre-fills the party/terms from whatever's already assigned on the Opportunity
            // (see Opportunities/Edit) - the common case where the deal's Partner/Agent was
            // already decided, so recording the commission is a one-click confirmation.
            var opportunity = await _opportunityRepository.GetAsync(OpportunityId.Value);
            Commission.PartnerId = opportunity.PartnerId;
            Commission.AgentId = opportunity.AgentId;

            if (opportunity.PartnerId.HasValue)
            {
                var partner = await _partnerRepository.GetAsync(opportunity.PartnerId.Value);
                Commission.Basis = partner.CommissionBasis;
                Commission.Rate = partner.CommissionRate;
                Commission.Trigger = partner.CommissionTrigger;
            }
            else if (opportunity.AgentId.HasValue)
            {
                var agent = await _agentRepository.GetAsync(opportunity.AgentId.Value);
                Commission.Basis = agent.CommissionBasis;
                Commission.Rate = agent.CommissionRate;
                Commission.Trigger = agent.CommissionTrigger;
            }

            Commission.BaseAmount = opportunity.EstimatedValue ?? 0;
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _commissionAppService.CreateAsync(Commission);
        return RedirectToPage("./Index", new { OpportunityId = Commission.OpportunityId });
    }

    private async Task LoadOptionsAsync()
    {
        var opportunities = await _opportunityRepository.GetListAsync();
        OpportunityOptions = opportunities
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var partners = await _partnerRepository.GetListAsync();
        PartnerOptions = new List<SelectListItem> { new(L["None"], "") };
        PartnerOptions.AddRange(partners.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));

        var agents = await _agentRepository.GetListAsync();
        AgentOptions = new List<SelectListItem> { new(L["None"], "") };
        AgentOptions.AddRange(agents.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
    }
}
