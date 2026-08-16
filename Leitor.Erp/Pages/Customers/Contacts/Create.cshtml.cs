using System;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
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

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("/Customers/Detail", new { id = CustomerId }) });
        }

        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }
}
