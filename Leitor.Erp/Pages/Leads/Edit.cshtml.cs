using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Edit)]
public class EditModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
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

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateLeadDto Lead { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> AgentOptions { get; set; } = new();
    public bool ShowReferrerAgent { get; set; }

    public async Task OnGetAsync()
    {
        var lead = await _leadAppService.GetAsync(Id);
        Lead = new CreateUpdateLeadDto
        {
            Name = lead.Name,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            Source = lead.Source,
            Status = lead.Status,
            AssignedToUserId = lead.AssignedToUserId,
            Notes = lead.Notes,
            DoNotContact = lead.DoNotContact,
            ReferrerAgentId = lead.ReferrerAgentId
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

        try
        {
            await _leadAppService.UpdateAsync(Id, Lead);
        }
        catch (UserFriendlyException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadOptionsAsync();
            return Page();
        }

        this.SetSuccessMessage(L["LeadUpdatedSuccessfully"]);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = Id }) });
        }

        return RedirectToPage("./Detail", new { id = Id });
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
