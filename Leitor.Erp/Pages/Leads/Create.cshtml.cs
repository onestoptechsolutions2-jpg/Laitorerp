using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Create)]
public class CreateModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        LeadAppService leadAppService,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Agent, Guid> agentRepository,
        IFeatureChecker featureChecker)
    {
        _leadAppService = leadAppService;
        _identityUserRepository = identityUserRepository;
        _agentRepository = agentRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateLeadDto Lead { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> AgentOptions { get; set; } = new();
    public bool ShowReferrerAgent { get; set; }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var lead = await _leadAppService.CreateAsync(Lead);
        return RedirectToPage("./Detail", new { id = lead.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );

        ShowReferrerAgent = await _featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission);
        if (ShowReferrerAgent)
        {
            var agents = await _agentRepository.GetListAsync();
            AgentOptions = new List<SelectListItem> { new(L["None"], "") };
            AgentOptions.AddRange(agents.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
        }
    }
}
