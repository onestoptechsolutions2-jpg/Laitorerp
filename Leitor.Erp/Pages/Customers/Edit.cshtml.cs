using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class EditModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;

    public EditModel(CustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateCustomerDto Customer { get; set; } = new();

    public async Task OnGetAsync()
    {
        var customer = await _customerAppService.GetAsync(Id);
        Customer = new CreateUpdateCustomerDto
        {
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            AddressLine = customer.AddressLine,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode,
            Country = customer.Country,
            Status = customer.Status,
            Notes = customer.Notes
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _customerAppService.UpdateAsync(Id, Customer);
        return RedirectToPage("./Detail", new { id = Id });
    }
}
