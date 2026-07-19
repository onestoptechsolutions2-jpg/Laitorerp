using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Create)]
public class CreateModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public CreateModel(
        OpportunityAppService opportunityAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _opportunityAppService = opportunityAppService;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty]
    public CreateUpdateOpportunityDto Opportunity { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task OnGetAsync(Guid? customerId)
    {
        if (customerId.HasValue)
        {
            Opportunity.CustomerId = customerId.Value;
        }

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var opportunity = await _opportunityAppService.CreateAsync(Opportunity);
        return RedirectToPage("./Detail", new { id = opportunity.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }
}
