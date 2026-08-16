using System;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Procurement.Vendors.Contacts;

[Authorize(Policy = ErpPermissions.Vendors.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly VendorContactAppService _vendorContactAppService;

    public CreateModel(VendorContactAppService vendorContactAppService)
    {
        _vendorContactAppService = vendorContactAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VendorId { get; set; }

    [BindProperty]
    public CreateUpdateVendorContactDto Contact { get; set; } = new();

    public void OnGet()
    {
        Contact.VendorId = VendorId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contact.VendorId = VendorId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _vendorContactAppService.CreateAsync(Contact);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("/Procurement/Vendors/Detail", new { id = VendorId }) });
        }

        return RedirectToPage("/Procurement/Vendors/Detail", new { id = VendorId });
    }
}