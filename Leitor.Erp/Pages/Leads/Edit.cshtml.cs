using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Edit)]
public class EditModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public EditModel(
        LeadAppService leadAppService,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _leadAppService = leadAppService;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateLeadDto Lead { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var lead = await _leadAppService.GetAsync(Id);
        Lead = new CreateUpdateLeadDto
        {
            Name = lead.Name,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            Source = lead.Source,
            Status = lead.Status,
            AssignedToUserId = lead.AssignedToUserId,
            Notes = lead.Notes,
            DoNotContact = lead.DoNotContact
        };

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _leadAppService.UpdateAsync(Id, Lead);
        return RedirectToPage("./Detail", new { id = Id });
    }

    private async Task LoadOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }
}
