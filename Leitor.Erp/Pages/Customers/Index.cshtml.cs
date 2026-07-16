using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Default)]
public class IndexModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;

    public IndexModel(CustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public IReadOnlyList<CustomerDto> Customers { get; set; } = Array.Empty<CustomerDto>();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Delete);

        var result = await _customerAppService.GetListAsync(new GetCustomerListInput
        {
            Filter = Filter,
            MaxResultCount = 1000
        });

        Customers = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _customerAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter });
    }
}
