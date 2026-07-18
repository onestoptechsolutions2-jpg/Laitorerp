using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Procurement.Vendors;

[Authorize(Policy = ErpPermissions.Vendors.Edit)]
public class EditModel : AbpPageModel
{
    private readonly VendorAppService _vendorAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public EditModel(
        VendorAppService vendorAppService,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _vendorAppService = vendorAppService;
        _identityUserRepository = identityUserRepository;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateVendorDto Vendor { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Vendor", Id);

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
            Notes = vendor.Notes,
            PortalUserId = vendor.PortalUserId
        };

        await LoadUserOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserOptionsAsync();
            return Page();
        }

        await _vendorAppService.UpdateAsync(Id, Vendor);
        return RedirectToPage("./Index");
    }

    private async Task LoadUserOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }
}
