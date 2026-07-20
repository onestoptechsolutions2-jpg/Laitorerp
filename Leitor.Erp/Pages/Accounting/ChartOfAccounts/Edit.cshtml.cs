using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.ChartOfAccounts;

[Authorize(Policy = ErpPermissions.Accounting.Edit)]
public class EditModel : AbpPageModel
{
    private readonly AccountAppService _accountAppService;

    public EditModel(AccountAppService accountAppService)
    {
        _accountAppService = accountAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateAccountDto Account { get; set; } = new();

    public async Task OnGetAsync()
    {
        var account = await _accountAppService.GetAsync(Id);
        Account = new CreateUpdateAccountDto
        {
            Code = account.Code,
            Name = account.Name,
            Type = account.Type,
            SystemRole = account.SystemRole,
            IsActive = account.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _accountAppService.UpdateAsync(Id, Account);
        return RedirectToPage("./Index");
    }
}
