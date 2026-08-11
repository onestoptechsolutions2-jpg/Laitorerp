using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Contracts.Templates;

[Authorize(Policy = ErpPermissions.Customers.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ContractTemplateAppService _contractTemplateAppService;

    public IndexModel(ContractTemplateAppService contractTemplateAppService)
    {
        _contractTemplateAppService = contractTemplateAppService;
    }

    public List<ContractTemplateDto> Templates { get; set; } = new();
    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);
        Templates = await _contractTemplateAppService.GetListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _contractTemplateAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
