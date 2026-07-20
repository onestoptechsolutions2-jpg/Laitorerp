using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.ChartOfAccounts;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly AccountAppService _accountAppService;

    public IndexModel(AccountAppService accountAppService)
    {
        _accountAppService = accountAppService;
    }

    public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Edit);

        var result = await _accountAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 1000,
            Sorting = "Code"
        });
        Accounts = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _accountAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
