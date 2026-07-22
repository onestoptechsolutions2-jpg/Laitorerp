using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Read-only aggregation, plain ApplicationService - same convention as GeneralLedgerReportAppService,
// whose LedgerMath.ComputeAccountNets helper this reuses for the Net Income figure. Built last in
// the accounting-module build since it's the only report that needs Depreciation (Fixed Assets
// phase) to already exist for the add-back line.
public class CashFlowReportAppService : ApplicationService
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<DepreciationEntry, Guid> _depreciationEntryRepository;
    private readonly IRepository<FixedAsset, Guid> _fixedAssetRepository;

    public CashFlowReportAppService(
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<DepreciationEntry, Guid> depreciationEntryRepository,
        IRepository<FixedAsset, Guid> fixedAssetRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _depreciationEntryRepository = depreciationEntryRepository;
        _fixedAssetRepository = fixedAssetRepository;
    }

    public async Task<CashFlowDto> GetCashFlowAsync(DateTime fromDate, DateTime toDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var accounts = await _accountRepository.GetListAsync();

        var periodEntries = await _journalEntryRepository.GetListAsync(x => x.EntryDate >= fromDate && x.EntryDate <= toDate);
        var periodEntryIds = periodEntries.Select(x => x.Id).ToList();
        var periodLines = periodEntryIds.Count > 0
            ? await _journalEntryLineRepository.GetListAsync(x => periodEntryIds.Contains(x.JournalEntryId))
            : new List<JournalEntryLine>();

        var (revenueNets, expenseNets) = LedgerMath.ComputeAccountNets(periodLines, accounts);
        var netIncome = revenueNets.Sum(x => x.Amount) - expenseNets.Sum(x => x.Amount);

        var depreciation = (await _depreciationEntryRepository.GetListAsync(x => x.PeriodDate >= fromDate && x.PeriodDate <= toDate))
            .Sum(x => x.Amount);

        var arChange = await ComputeRoleBalanceChangeAsync(accounts, SystemAccountRole.AccountsReceivable, fromDate, toDate, isAsset: true);
        var apChange = await ComputeRoleBalanceChangeAsync(accounts, SystemAccountRole.AccountsPayable, fromDate, toDate, isAsset: false);
        var inventoryChange = await ComputeRoleBalanceChangeAsync(accounts, SystemAccountRole.Inventory, fromDate, toDate, isAsset: true);

        // An asset balance increasing ties up cash (outflow); a liability balance increasing frees
        // up cash (inflow, since it means less has been paid out yet) - standard indirect-method
        // sign convention.
        var arCashEffect = -arChange;
        var apCashEffect = apChange;
        var inventoryCashEffect = -inventoryChange;

        var operatingCashFlow = netIncome + depreciation + arCashEffect + apCashEffect + inventoryCashEffect;

        var investingCashFlow = -(await _fixedAssetRepository.GetListAsync(x => x.PurchaseDate >= fromDate && x.PurchaseDate <= toDate))
            .Sum(x => x.PurchaseCost);

        const decimal financingCashFlow = 0;

        var netChangeInCash = operatingCashFlow + investingCashFlow + financingCashFlow;
        var actualCashChange = await ComputeRoleBalanceChangeAsync(accounts, SystemAccountRole.Cash, fromDate, toDate, isAsset: true);

        return new CashFlowDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            NetIncome = netIncome,
            DepreciationAddBack = depreciation,
            AccountsReceivableChange = arCashEffect,
            AccountsPayableChange = apCashEffect,
            InventoryChange = inventoryCashEffect,
            NetCashFromOperating = operatingCashFlow,
            NetCashFromInvesting = investingCashFlow,
            NetCashFromFinancing = financingCashFlow,
            NetChangeInCash = netChangeInCash,
            ActualCashChange = actualCashChange
        };
    }

    // Balance at toDate minus balance at fromDate (exclusive of fromDate itself, i.e. the opening
    // balance already includes everything up to and including fromDate) for the one Account
    // carrying the given SystemAccountRole.
    private async Task<decimal> ComputeRoleBalanceChangeAsync(List<Account> accounts, SystemAccountRole role, DateTime fromDate, DateTime toDate, bool isAsset)
    {
        var account = accounts.FirstOrDefault(x => x.SystemRole == role);
        if (account == null)
        {
            return 0;
        }

        var openingBalance = await ComputeAccountBalanceAsync(account.Id, fromDate, isAsset);
        var closingBalance = await ComputeAccountBalanceAsync(account.Id, toDate, isAsset);
        return closingBalance - openingBalance;
    }

    private async Task<decimal> ComputeAccountBalanceAsync(Guid accountId, DateTime asOfDate, bool isAsset)
    {
        var entries = await _journalEntryRepository.GetListAsync(x => x.EntryDate <= asOfDate);
        var entryIds = entries.Select(x => x.Id).ToList();
        if (entryIds.Count == 0)
        {
            return 0;
        }

        var lines = await _journalEntryLineRepository.GetListAsync(x => x.AccountId == accountId && entryIds.Contains(x.JournalEntryId));
        var debitTotal = lines.Sum(x => x.Debit * x.ExchangeRateToBase);
        var creditTotal = lines.Sum(x => x.Credit * x.ExchangeRateToBase);

        return isAsset ? debitTotal - creditTotal : creditTotal - debitTotal;
    }
}
