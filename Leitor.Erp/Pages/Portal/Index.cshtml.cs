using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using VendorEntity = Leitor.Erp.Entities.Procurement.Vendor;

namespace Leitor.Erp.Pages.Portal;

// Single entry point for the "My Portal" menu link - routes a logged-in user to whichever portal
// they're actually linked to (Client or Vendor), so staff don't need two separate menu items and
// the link works the same regardless of which kind of portal account is logged in.
[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<VendorEntity, Guid> _vendorRepository;

    public IndexModel(
        IRepository<Customer, Guid> customerRepository,
        IRepository<VendorEntity, Guid> vendorRepository)
    {
        _customerRepository = customerRepository;
        _vendorRepository = vendorRepository;
    }

    public bool IsLinked { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            IsLinked = false;
            return Page();
        }

        var hasCustomerLink = (await _customerRepository.GetListAsync(x => x.PortalUserId == CurrentUser.Id.Value)).Any();
        if (hasCustomerLink)
        {
            return RedirectToPage("/Portal/Client/Index");
        }

        var hasVendorLink = (await _vendorRepository.GetListAsync(x => x.PortalUserId == CurrentUser.Id.Value)).Any();
        if (hasVendorLink)
        {
            return RedirectToPage("/Portal/Vendor/Index");
        }

        IsLinked = false;
        return Page();
    }
}
