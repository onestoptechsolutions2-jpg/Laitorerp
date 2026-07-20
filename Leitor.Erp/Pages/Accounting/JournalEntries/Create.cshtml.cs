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

namespace Leitor.Erp.Pages.Accounting.JournalEntries;

// A manual journal entry is only meaningful as a single balanced transaction, so - unlike every
// other line-entity in this app (added one at a time on a Detail page) - all of its lines are
// entered and submitted together here. A fixed number of blank line rows is pre-seeded rather
// than a JS-driven dynamic add/remove, mirroring how PurchaseOrders/Detail's goods-receipt form
// renders one fixed row per line via the same `asp-for="X.Lines[i].Y"` indexer pattern; empty
// rows (no debit or credit entered) are filtered out server-side.
[Authorize(Policy = ErpPermissions.Accounting.Edit)]
public class CreateModel : AbpPageModel
{
    private const int BlankLineCount = 8;

    private readonly JournalEntryAppService _journalEntryAppService;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public CreateModel(
        JournalEntryAppService journalEntryAppService,
        IRepository<Account, Guid> accountRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _journalEntryAppService = journalEntryAppService;
        _accountRepository = accountRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty]
    public CreateJournalEntryDto Entry { get; set; } = new()
    {
        EntryDate = DateTime.Today
    };

    public List<SelectListItem> AccountOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();
    public string BaseCurrencyCode { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        for (var i = 0; i < BlankLineCount; i++)
        {
            Entry.Lines.Add(new CreateJournalEntryLineDto { CurrencyCode = BaseCurrencyCode });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        // Rows left blank (no debit or credit entered) are just unused rows in the form, not
        // validation failures - JournalEntryAppService.CreateAsync applies this same filter, but
        // filtering here first keeps the "at least two lines" check meaningful.
        Entry.Lines = Entry.Lines.Where(x => x.Debit > 0 || x.Credit > 0).ToList();

        var entry = await _journalEntryAppService.CreateAsync(Entry);
        return RedirectToPage("./Detail", new { id = entry.Id });
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
        BaseCurrencyCode = currencies.FirstOrDefault(x => x.IsBaseCurrency)?.Code ?? string.Empty;
    }
}
