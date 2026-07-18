using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Default)]
public class DetailModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;

    public DetailModel(LeadAppService leadAppService)
    {
        _leadAppService = leadAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public LeadDto Lead { get; set; } = null!;

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Edit);
        Lead = await _leadAppService.GetAsync(Id);
    }

    public async Task<IActionResult> OnPostConvertToCustomerAsync()
    {
        var customerId = await _leadAppService.ConvertToCustomerAsync(Id);
        return RedirectToPage("/Customers/Detail", new { id = customerId });
    }
}
