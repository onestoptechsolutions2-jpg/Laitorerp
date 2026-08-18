using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Partners;

[Authorize(Policy = ErpPermissions.Partners.Create)]
public class CreateModel : AbpPageModel
{
    private readonly PartnerAppService _partnerAppService;

    public CreateModel(PartnerAppService partnerAppService)
    {
        _partnerAppService = partnerAppService;
    }

    [BindProperty]
    public CreateUpdatePartnerDto Partner { get; set; } = new();

    public IActionResult OnGet()
    {
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
