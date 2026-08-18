using System;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Partners;

[Authorize(Policy = ErpPermissions.Partners.Edit)]
public class EditModel : AbpPageModel
{
    private readonly PartnerAppService _partnerAppService;

    public EditModel(PartnerAppService partnerAppService)
    {
        _partnerAppService = partnerAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdatePartnerDto Partner { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var partner = await _partnerAppService.GetAsync(Id);
        Partner = new CreateUpdatePartnerDto
        {
            Name = partner.Name,
            Category = partner.Category,
            Email = partner.Email,
            Phone = partner.Phone,
            Notes = partner.Notes,
            CommissionBasis = partner.CommissionBasis,
            CommissionRate = partner.CommissionRate,
            CommissionTrigger = partner.CommissionTrigger
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _partnerAppService.UpdateAsync(Id, Partner);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Index") });
        }

        return RedirectToPage("./Index");
    }
}
