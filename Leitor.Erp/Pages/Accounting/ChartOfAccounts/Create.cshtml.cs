using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.ChartOfAccounts;

[Authorize(Policy = ErpPermissions.Accounting.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly AccountAppService _accountAppService;

    public CreateModel(AccountAppService accountAppService)
    {
        _accountAppService = accountAppService;
    }

    [BindProperty]
    public CreateUpdateAccountDto Account { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _accountAppService.CreateAsync(Account);
        return RedirectToPage("./Index");
    }
}
