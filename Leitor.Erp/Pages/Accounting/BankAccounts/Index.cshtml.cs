using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.BankAccounts;

[Authorize(Policy = ErpPermissions.Banking.Default)]
public class IndexModel : AbpPageModel
{
    private readonly BankAccountAppService _bankAccountAppService;

    public IndexModel(BankAccountAppService bankAccountAppService)
    {
        _bankAccountAppService = bankAccountAppService;
    }

    public IReadOnlyList<BankAccountDto> BankAccounts { get; set; } = Array.Empty<BankAccountDto>();
    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Banking.Edit);

        var result = await _bankAccountAppService.GetListAsync(new GetBankAccountListInput
        {
            MaxResultCount = 1000,
            Sorting = "Name"
        });
        BankAccounts = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _bankAccountAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
