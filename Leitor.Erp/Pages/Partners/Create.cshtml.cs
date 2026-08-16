using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Partners;

[Authorize(Policy = ErpPermissions.Partners.Create)]
public class CreateModel : AbpPageModel
{
    private readonly PartnerAppService _partnerAppService;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(PartnerAppService partnerAppService, IFeatureChecker featureChecker)
    {
        _partnerAppService = partnerAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdatePartnerDto Partner { get; set; } = new();

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

        await _partnerAppService.CreateAsync(Partner);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Index") });
        }

        return RedirectToPage("./Index");
    }
}
