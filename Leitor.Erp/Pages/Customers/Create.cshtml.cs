using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Create)]
public class CreateModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;

    public CreateModel(CustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    [BindProperty]
    public CreateUpdateCustomerDto Customer { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var customer = await _customerAppService.CreateAsync(Customer);
        return RedirectToPage("./Detail", new { id = customer.Id });
    }
}
