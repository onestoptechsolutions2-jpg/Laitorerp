using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Accounting.BankAccounts;

[Authorize(Policy = ErpPermissions.Banking.Edit)]
public class EditModel : AbpPageModel
{
    private readonly BankAccountAppService _bankAccountAppService;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public EditModel(
        BankAccountAppService bankAccountAppService,
        IRepository<Account, Guid> accountRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _bankAccountAppService = bankAccountAppService;
        _accountRepository = accountRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateBankAccountDto BankAccount { get; set; } = new();

    public List<SelectListItem> AccountOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        var bankAccount = await _bankAccountAppService.GetAsync(Id);
        BankAccount = new CreateUpdateBankAccountDto
        {
            Name = bankAccount.Name,
            AccountNumber = bankAccount.AccountNumber,
            BankName = bankAccount.BankName,
            CurrencyCode = bankAccount.CurrencyCode,
            LinkedGlAccountId = bankAccount.LinkedGlAccountId,
            OpeningBalance = bankAccount.OpeningBalance,
            OpeningBalanceDate = bankAccount.OpeningBalanceDate
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _bankAccountAppService.UpdateAsync(Id, BankAccount);
        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync()
    {
        var accounts = await _accountRepository.GetListAsync(x => x.IsActive);
        AccountOptions = accounts
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();

        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive);
        CurrencyOptions = currencies
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code, x.Code))
            .ToList();
    }
}
