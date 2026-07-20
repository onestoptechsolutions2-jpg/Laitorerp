using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Currencies;

[Authorize(Policy = ErpPermissions.Accounting.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly CurrencyAppService _currencyAppService;

    public CreateModel(CurrencyAppService currencyAppService)
    {
        _currencyAppService = currencyAppService;
    }

    [BindProperty]
    public CreateUpdateCurrencyDto Currency { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _currencyAppService.CreateAsync(Currency);
        return RedirectToPage("./Index");
    }
}
