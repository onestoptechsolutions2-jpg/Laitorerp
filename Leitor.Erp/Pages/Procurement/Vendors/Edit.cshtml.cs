using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Procurement.Vendors;

[Authorize(Policy = ErpPermissions.Vendors.Edit)]
public class EditModel : AbpPageModel
{
    private readonly VendorAppService _vendorAppService;

    public EditModel(VendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateVendorDto Vendor { get; set; } = new();

    public async Task OnGetAsync()
    {
        var vendor = await _vendorAppService.GetAsync(Id);
        Vendor = new CreateUpdateVendorDto
        {
            Name = vendor.Name,
            Email = vendor.Email,
            Phone = vendor.Phone,
            AddressLine = vendor.AddressLine,
            City = vendor.City,
            State = vendor.State,
            PostalCode = vendor.PostalCode,
            Country = vendor.Country,
            Notes = vendor.Notes
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _vendorAppService.UpdateAsync(Id, Vendor);
        return RedirectToPage("./Index");
    }
}
