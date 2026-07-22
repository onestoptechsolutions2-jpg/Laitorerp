using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.BankAccounts;

[Authorize(Policy = ErpPermissions.Banking.Default)]
public class ReconcileModel : AbpPageModel
{
    private readonly BankAccountAppService _bankAccountAppService;
    private readonly BankReconciliationAppService _bankReconciliationAppService;

    public ReconcileModel(BankAccountAppService bankAccountAppService, BankReconciliationAppService bankReconciliationAppService)
    {
        _bankAccountAppService = bankAccountAppService;
        _bankReconciliationAppService = bankReconciliationAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public BankAccountDto BankAccount { get; set; } = null!;
    public List<BankStatementLineDto> UnreconciledStatementLines { get; set; } = new();
    public List<UnreconciledGlLineDto> UnreconciledGlLines { get; set; } = new();
    public BankReconciliationSummaryDto Summary { get; set; } = new();
    public bool CanEdit { get; set; }

    [BindProperty]
    public string ImportCsvText { get; set; } = string.Empty;

    [BindProperty]
    public Guid MatchStatementLineId { get; set; }

    [BindProperty]
    public Guid MatchJournalEntryLineId { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Banking.Edit);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        try
        {
            var count = await _bankReconciliationAppService.ImportStatementLinesAsync(Id, ImportCsvText);
            SuccessMessage = $"Imported {count} statement line(s).";
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostMatchAsync()
    {
        try
        {
            await _bankReconciliationAppService.MatchAsync(MatchStatementLineId, MatchJournalEntryLineId);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }

    private async Task LoadAsync()
    {
        BankAccount = await _bankAccountAppService.GetAsync(Id);
        UnreconciledStatementLines = await _bankReconciliationAppService.GetUnreconciledStatementLinesAsync(Id);
        UnreconciledGlLines = await _bankReconciliationAppService.GetUnreconciledGlLinesAsync(Id);
        Summary = await _bankReconciliationAppService.GetSummaryAsync(Id, Clock.Now);
    }
}
