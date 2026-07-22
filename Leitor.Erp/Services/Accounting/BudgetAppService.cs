using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Budgets are entered via a spreadsheet-style grid (Accounts x 12 months for one FiscalYear) - a
// CrudAppService per single Budget row would need 96+ round trips to save a year, so this is a
// plain ApplicationService with one load and one bulk-save operation instead.
public class BudgetAppService : ApplicationService
{
    private readonly IRepository<Budget, Guid> _budgetRepository;
    private readonly IRepository<Account, Guid> _accountRepository;

    public BudgetAppService(IRepository<Budget, Guid> budgetRepository, IRepository<Account, Guid> accountRepository)
    {
        _budgetRepository = budgetRepository;
        _accountRepository = accountRepository;
    }

    public async Task<BudgetGridDto> GetGridAsync(int fiscalYear)
    {
        await CheckPolicyAsync(ErpPermissions.Budgets.Default);

        var accounts = (await _accountRepository.GetListAsync(x => x.IsActive && (x.Type == AccountType.Revenue || x.Type == AccountType.Expense)))
            .OrderBy(x => x.Code)
            .ToList();

        var budgets = await _budgetRepository.GetListAsync(x => x.FiscalYear == fiscalYear);
        var budgetsByAccountId = budgets.ToLookup(x => x.AccountId);

        var grid = new BudgetGridDto { FiscalYear = fiscalYear };
        foreach (var account in accounts)
        {
            var row = new BudgetGridRowDto { AccountId = account.Id, AccountCode = account.Code, AccountName = account.Name };
            foreach (var budget in budgetsByAccountId[account.Id])
            {
                if (budget.Month is >= 1 and <= 12)
                {
                    row.MonthAmounts[budget.Month - 1] = budget.Amount;
                }
            }

            grid.Rows.Add(row);
        }

        return grid;
    }

    public async Task SaveGridAsync(SaveBudgetGridDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Budgets.Edit);

        var existing = await _budgetRepository.GetListAsync(x => x.FiscalYear == input.FiscalYear);
        var existingByKey = existing.ToDictionary(x => (x.AccountId, x.Month));

        foreach (var cell in input.Cells)
        {
            var key = (cell.AccountId, cell.Month);
            if (existingByKey.TryGetValue(key, out var budget))
            {
                // Updated in place rather than deleted-when-zeroed: Budget's (AccountId,
                // FiscalYear, Month) unique index would otherwise still be occupied by the
                // soft-deleted row (ABP's ISoftDelete query filter hides it from GetListAsync,
                // but the row - and its unique index entry - still physically exists), so
                // re-entering a value later would throw a duplicate-key error the same way
                // Services/DocumentNumbering.cs's own comment describes for numbered documents.
                if (budget.Amount != cell.Amount)
                {
                    budget.Amount = cell.Amount;
                    await _budgetRepository.UpdateAsync(budget, autoSave: true);
                }
            }
            else if (cell.Amount != 0)
            {
                await _budgetRepository.InsertAsync(
                    new Budget(GuidGenerator.Create(), cell.AccountId, input.FiscalYear, cell.Month, cell.Amount),
                    autoSave: true);
            }
        }
    }
}
