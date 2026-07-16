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

namespace Leitor.Erp.Pages.Customers.Tasks;

// Property is "TaskInput", not "Task" - this class needs System.Threading.Tasks.Task as a
// return type, and a same-named property would shadow it within the class.
[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly CustomerTaskAppService _customerTaskAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public CreateModel(
        CustomerTaskAppService customerTaskAppService,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _customerTaskAppService = customerTaskAppService;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    [BindProperty]
    public CreateUpdateCustomerTaskDto TaskInput { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        TaskInput.CustomerId = CustomerId;
        await LoadUserOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TaskInput.CustomerId = CustomerId;

        if (!ModelState.IsValid)
        {
            await LoadUserOptionsAsync();
            return Page();
        }

        await _customerTaskAppService.CreateAsync(TaskInput);
        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
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
