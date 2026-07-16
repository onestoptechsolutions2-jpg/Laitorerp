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
public class EditModel : AbpPageModel
{
    private readonly CustomerContactAppService _customerContactAppService;

    public EditModel(CustomerContactAppService customerContactAppService)
    {
        _customerContactAppService = customerContactAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    [BindProperty]
    public CreateUpdateCustomerContactDto Contact { get; set; } = new();

    public async Task OnGetAsync()
    {
        var contact = await _customerContactAppService.GetAsync(Id);
        Contact = new CreateUpdateCustomerContactDto
        {
            CustomerId = contact.CustomerId,
            FullName = contact.FullName,
            JobTitle = contact.JobTitle,
            Email = contact.Email,
            PhoneNumber = contact.PhoneNumber,
            IsPrimary = contact.IsPrimary,
            Notes = contact.Notes
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contact.CustomerId = CustomerId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _customerContactAppService.UpdateAsync(Id, Contact);
        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }
}
