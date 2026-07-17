using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Procurement.Vendors;

[Authorize(Policy = ErpPermissions.Vendors.Create)]
public class CreateModel : AbpPageModel
{
    private readonly VendorAppService _vendorAppService;

    public CreateModel(VendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    [BindProperty]
    public CreateUpdateVendorDto Vendor { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _vendorAppService.CreateAsync(Vendor);
        return RedirectToPage("./Index");
    }
}
