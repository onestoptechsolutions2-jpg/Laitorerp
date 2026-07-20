using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Currencies;

[Authorize(Policy = ErpPermissions.Accounting.Edit)]
public class EditModel : AbpPageModel
{
    private readonly CurrencyAppService _currencyAppService;

    public EditModel(CurrencyAppService currencyAppService)
    {
        _currencyAppService = currencyAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateCurrencyDto Currency { get; set; } = new();

    public async Task OnGetAsync()
    {
        var currency = await _currencyAppService.GetAsync(Id);
        Currency = new CreateUpdateCurrencyDto
        {
            Code = currency.Code,
            Name = currency.Name,
            Symbol = currency.Symbol,
            IsBaseCurrency = currency.IsBaseCurrency,
            IsActive = currency.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _currencyAppService.UpdateAsync(Id, Currency);
        return RedirectToPage("./Index");
    }
}
