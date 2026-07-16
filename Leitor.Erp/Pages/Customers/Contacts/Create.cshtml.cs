using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers.Contacts;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly CustomerContactAppService _customerContactAppService;

    public CreateModel(CustomerContactAppService customerContactAppService)
    {
        _customerContactAppService = customerContactAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    [BindProperty]
    public CreateUpdateCustomerContactDto Contact { get; set; } = new();

    public void OnGet()
    {
        Contact.CustomerId = CustomerId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contact.CustomerId = CustomerId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _customerContactAppService.CreateAsync(Contact);
        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }
}
