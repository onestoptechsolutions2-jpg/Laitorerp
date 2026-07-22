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

namespace Leitor.Erp.Pages.Accounting.RecurringJournals;

[Authorize(Policy = ErpPermissions.Accounting.Edit)]
public class EditModel : AbpPageModel
{
    private const int ExtraBlankLineCount = 2;

    private readonly RecurringJournalTemplateAppService _templateAppService;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public EditModel(
        RecurringJournalTemplateAppService templateAppService,
        IRepository<Account, Guid> accountRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _templateAppService = templateAppService;
        _accountRepository = accountRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateRecurringJournalTemplateDto Template { get; set; } = new();

    public List<SelectListItem> AccountOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();
    public string BaseCurrencyCode { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        var template = await _templateAppService.GetAsync(Id);
        Template = new CreateUpdateRecurringJournalTemplateDto
        {
            Description = template.Description,
            Frequency = template.Frequency,
            NextRunDate = template.NextRunDate,
            IsActive = template.IsActive,
            Lines = template.Lines.Select(x => new CreateUpdateRecurringJournalTemplateLineDto
            {
                AccountId = x.AccountId,
                Debit = x.Debit,
                Credit = x.Credit,
                CurrencyCode = x.CurrencyCode
            }).ToList()
        };

        for (var i = 0; i < ExtraBlankLineCount; i++)
        {
            Template.Lines.Add(new CreateUpdateRecurringJournalTemplateLineDto { CurrencyCode = BaseCurrencyCode });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        Template.Lines = Template.Lines.Where(x => x.Debit > 0 || x.Credit > 0).ToList();

        await _templateAppService.UpdateAsync(Id, Template);
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
        BaseCurrencyCode = currencies.FirstOrDefault(x => x.IsBaseCurrency)?.Code ?? string.Empty;
    }
}
