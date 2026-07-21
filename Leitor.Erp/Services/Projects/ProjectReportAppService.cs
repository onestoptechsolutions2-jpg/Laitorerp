using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Projects;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Projects;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// GeneralLedgerReportAppService, whose ComputeNetIncomeByAccountAsync grouping logic this mirrors
// exactly, just scoped to JournalEntryLine.ProjectId instead of a date range. Nothing here stores
// a project's P&L anywhere - it's summed live from the GL every time, same "compute, never store"
// discipline as every other report in this app.
[RequiresFeature(ErpFeatures.ProjectManagement)]
public class ProjectReportAppService : ApplicationService
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;

    public ProjectReportAppService(
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
    }

    public async Task<ProjectPnLDto> GetProjectPnLAsync(Guid projectId)
    {
        await CheckPolicyAsync(ErpPermissions.Projects.Default);

        var lines = await _journalEntryLineRepository.GetListAsync(x => x.ProjectId == projectId);
        var accounts = await _accountRepository.GetListAsync();
        var accountsById = accounts.ToDictionary(x => x.Id);
        var linesByAccountId = lines.ToLookup(x => x.AccountId);

        var result = new ProjectPnLDto { ProjectId = projectId };

        foreach (var account in accounts)
        {
            if (account.Type is not (AccountType.Revenue or AccountType.Expense))
            {
                continue;
            }

            var accountLines = linesByAccountId[account.Id].ToList();
            if (accountLines.Count == 0)
            {
                continue;
            }

            var debitTotal = accountLines.Sum(x => x.Debit * x.ExchangeRateToBase);
            var creditTotal = accountLines.Sum(x => x.Credit * x.ExchangeRateToBase);

            if (account.Type == AccountType.Revenue)
            {
                var amount = creditTotal - debitTotal;
                if (amount != 0)
                {
                    result.RevenueLines.Add(new ProjectPnLLineDto { AccountCode = account.Code, AccountName = account.Name, Amount = amount });
                }
            }
            else
            {
                var amount = debitTotal - creditTotal;
                if (amount != 0)
                {
                    result.ExpenseLines.Add(new ProjectPnLLineDto { AccountCode = account.Code, AccountName = account.Name, Amount = amount });
                }
            }
        }

        result.RevenueLines = result.RevenueLines.OrderBy(x => x.AccountCode).ToList();
        result.ExpenseLines = result.ExpenseLines.OrderBy(x => x.AccountCode).ToList();
        result.TotalRevenue = result.RevenueLines.Sum(x => x.Amount);
        result.TotalExpense = result.ExpenseLines.Sum(x => x.Amount);
        result.NetProfit = result.TotalRevenue - result.TotalExpense;

        return result;
    }
}
