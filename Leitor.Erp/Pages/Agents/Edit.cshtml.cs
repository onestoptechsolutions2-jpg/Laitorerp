using System;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Agents;

[Authorize(Policy = ErpPermissions.Partners.Edit)]
public class EditModel : AbpPageModel
{
    private readonly AgentAppService _agentAppService;

    public EditModel(AgentAppService agentAppService)
    {
        _agentAppService = agentAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateAgentDto Agent { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var agent = await _agentAppService.GetAsync(Id);
        Agent = new CreateUpdateAgentDto
        {
            Name = agent.Name,
            Email = agent.Email,
            Phone = agent.Phone,
            Territory = agent.Territory,
            Skills = agent.Skills,
            Notes = agent.Notes,
            IsActive = agent.IsActive,
            CommissionBasis = agent.CommissionBasis,
            CommissionRate = agent.CommissionRate,
            CommissionTrigger = agent.CommissionTrigger
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _agentAppService.UpdateAsync(Id, Agent);
        }
        catch (UserFriendlyException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        this.SetSuccessMessage(L["AgentUpdatedSuccessfully"]);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Index") });
        }

        return RedirectToPage("./Index");
    }
}
