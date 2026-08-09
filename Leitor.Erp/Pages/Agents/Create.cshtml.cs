using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Agents;

[Authorize(Policy = ErpPermissions.Partners.Create)]
public class CreateModel : AbpPageModel
{
    private readonly AgentAppService _agentAppService;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(AgentAppService agentAppService, IFeatureChecker featureChecker)
    {
        _agentAppService = agentAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateAgentDto Agent { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _agentAppService.CreateAsync(Agent);
        return RedirectToPage("./Index");
    }
}
