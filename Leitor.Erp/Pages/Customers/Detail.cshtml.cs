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
public class DetailModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;
    private readonly CustomerContactAppService _customerContactAppService;
    private readonly CustomerContractAppService _customerContractAppService;

    public DetailModel(
        CustomerAppService customerAppService,
        CustomerContactAppService customerContactAppService,
        CustomerContractAppService customerContractAppService)
    {
        _customerAppService = customerAppService;
        _customerContactAppService = customerContactAppService;
        _customerContractAppService = customerContractAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public CustomerDto Customer { get; set; } = null!;
    public IReadOnlyList<CustomerContactDto> Contacts { get; set; } = Array.Empty<CustomerContactDto>();
    public IReadOnlyList<CustomerContractDto> Contracts { get; set; } = Array.Empty<CustomerContractDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);

        Customer = await _customerAppService.GetAsync(Id);

        var contacts = await _customerContactAppService.GetListAsync(new GetCustomerContactListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        Contacts = contacts.Items;

        var contracts = await _customerContractAppService.GetListAsync(new GetCustomerContractListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        Contracts = contracts.Items;
    }

    public async Task<IActionResult> OnPostDeleteContactAsync(Guid contactId)
    {
        await _customerContactAppService.DeleteAsync(contactId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteContractAsync(Guid contractId)
    {
        await _customerContractAppService.DeleteAsync(contractId);
        return RedirectToPage(new { id = Id });
    }
}
