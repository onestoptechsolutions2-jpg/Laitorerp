using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers.Contracts;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly CustomerContractAppService _customerContractAppService;

    public CreateModel(CustomerContractAppService customerContractAppService)
    {
        _customerContractAppService = customerContractAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    [BindProperty]
    public CreateUpdateCustomerContractDto Contract { get; set; } = new()
    {
        StartDate = DateTime.Today
    };

    public void OnGet()
    {
        Contract.CustomerId = CustomerId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contract.CustomerId = CustomerId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _customerContractAppService.CreateAsync(Contract);
        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }
}
