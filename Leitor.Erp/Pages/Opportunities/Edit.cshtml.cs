using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class EditModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Partner, Guid> _partnerRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
        OpportunityAppService opportunityAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Partner, Guid> partnerRepository,
        IRepository<Agent, Guid> agentRepository,
        IFeatureChecker featureChecker)
    {
        _opportunityAppService = opportunityAppService;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _partnerRepository = partnerRepository;
        _agentRepository = agentRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateOpportunityDto Opportunity { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> PartnerOptions { get; set; } = new();
    public List<SelectListItem> AgentOptions { get; set; } = new();
    public bool ShowPartnerCommissionFields { get; set; }

    public async Task OnGetAsync()
    {
        var opportunity = await _opportunityAppService.GetAsync(Id);
        Opportunity = new CreateUpdateOpportunityDto
        {
            CustomerId = opportunity.CustomerId,
            Name = opportunity.Name,
            Status = opportunity.Status,
            EstimatedValue = opportunity.EstimatedValue,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            AssignedToUserId = opportunity.AssignedToUserId,
            LostReason = opportunity.LostReason,
            Notes = opportunity.Notes,
            PartnerId = opportunity.PartnerId,
            AgentId = opportunity.AgentId
        };

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _opportunityAppService.UpdateAsync(Id, Opportunity);
        return RedirectToPage("./Detail", new { id = Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );

        ShowPartnerCommissionFields = await _featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission);
        if (ShowPartnerCommissionFields)
        {
            var partners = await _partnerRepository.GetListAsync();
            PartnerOptions = new List<SelectListItem> { new(L["None"], "") };
            PartnerOptions.AddRange(partners.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));

            var agents = await _agentRepository.GetListAsync();
            AgentOptions = new List<SelectListItem> { new(L["None"], "") };
            AgentOptions.AddRange(agents.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
        }
    }
}
