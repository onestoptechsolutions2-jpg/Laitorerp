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
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Commissions;

[Authorize(Policy = ErpPermissions.Partners.Create)]
public class CreateModel : AbpPageModel
{
    private readonly CommissionAppService _commissionAppService;
    private readonly PartnerAppService _partnerAppService;
    private readonly AgentAppService _agentAppService;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Partner, Guid> _partnerRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        CommissionAppService commissionAppService,
        PartnerAppService partnerAppService,
        AgentAppService agentAppService,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Partner, Guid> partnerRepository,
        IRepository<Agent, Guid> agentRepository,
        IFeatureChecker featureChecker)
    {
        _commissionAppService = commissionAppService;
        _partnerAppService = partnerAppService;
        _agentAppService = agentAppService;
        _opportunityRepository = opportunityRepository;
        _partnerRepository = partnerRepository;
        _agentRepository = agentRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateCommissionDto Commission { get; set; } = new();

    public string? ErrorMessage { get; set; }

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

        try
        {
            await _commissionAppService.CreateAsync(Commission);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
            await LoadOptionsAsync();
            return Page();
        }

        return RedirectToPage("./Index", new { OpportunityId = Commission.OpportunityId });
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostCreatePartnerAsync([FromBody] CreatePartnerRequest request)
    {
        // AJAX handler for creating a new Partner from this form, same shape as the Product/
        // Vendor/Customer modals elsewhere - a commission with no eligible Partner/Agent to pick
        // is a dead end otherwise (see ValidateExactlyOnePartyIsSet in CommissionAppService).
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return new JsonResult(new { error = "Partner name is required" }) { StatusCode = 400 };
            }

            var partner = await _partnerAppService.CreateAsync(new CreateUpdatePartnerDto
            {
                Name = request.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
            });

            return new JsonResult(new { id = partner.Id, name = partner.Name });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Error creating partner: {ex.Message}" }) { StatusCode = 400 };
        }
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostCreateAgentAsync([FromBody] CreateAgentRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return new JsonResult(new { error = "Agent name is required" }) { StatusCode = 400 };
            }

            var agent = await _agentAppService.CreateAsync(new CreateUpdateAgentDto
            {
                Name = request.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
            });

            return new JsonResult(new { id = agent.Id, name = agent.Name });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Error creating agent: {ex.Message}" }) { StatusCode = 400 };
        }
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
