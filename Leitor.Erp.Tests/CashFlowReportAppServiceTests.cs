using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers GetCurrentCashBalanceAsync (2026-08-17) - a simple "as of today" cash figure, distinct
// from GetCashFlowAsync's period-based report. Reuses the same SystemAccountRole.Cash + journal
// line summation the existing report already relies on.
//
// DashboardAppService.GetAsync() itself is NOT covered here (deliberately) - it has never been
// tested before this feature, and calling it hits a pre-existing bug unrelated to this change:
// GetLeadStatsAsync/GetCustomerStatsAsync/etc. all use the same GetQueryableAsync()-then-later-
// execute pattern that's already been diagnosed and fixed twice this session (JournalEntryAppService,
// this class's own GetCurrentCashBalanceAsync avoids it) - ObjectDisposedException against the
// DbContext once the lazy queryable is actually enumerated, specific to this SQLite test harness's
// unit-of-work setup. Fixing all of DashboardAppService's query patterns is a separate, larger job
// - flagged, not silently expanded into here.
public class CashFlowReportAppServiceTests : ErpTestBase
{
    private async Task<Guid> GetAccountIdAsync(string code)
    {
        var accountRepository = GetRequiredService<IRepository<Account, Guid>>();
        return (await accountRepository.GetListAsync(x => x.Code == code)).Single().Id;
    }

    [Fact]
    public async Task GetCurrentCashBalanceAsync_Sums_Cash_Account_Journal_Lines()
    {
        await EnsureDatabaseCreatedAsync();
        var cashId = await GetAccountIdAsync("1000");
        var equityId = await GetAccountIdAsync("3000");

        var journalEntryAppService = GetRequiredService<JournalEntryAppService>();
        await journalEntryAppService.CreateAsync(new CreateJournalEntryDto
        {
            EntryDate = DateTime.UtcNow,
            Description = "Owner contribution",
            Lines = new()
            {
                new CreateJournalEntryLineDto { AccountId = cashId, Debit = 5000m, CurrencyCode = "KES" },
                new CreateJournalEntryLineDto { AccountId = equityId, Credit = 5000m, CurrencyCode = "KES" }
            }
        });
        await journalEntryAppService.CreateAsync(new CreateJournalEntryDto
        {
            EntryDate = DateTime.UtcNow,
            Description = "Owner draw",
            Lines = new()
            {
                new CreateJournalEntryLineDto { AccountId = equityId, Debit = 2000m, CurrencyCode = "KES" },
                new CreateJournalEntryLineDto { AccountId = cashId, Credit = 2000m, CurrencyCode = "KES" }
            }
        });

        var cashFlowReportAppService = GetRequiredService<CashFlowReportAppService>();
        var balance = await cashFlowReportAppService.GetCurrentCashBalanceAsync();

        Assert.Equal(3000m, balance);
    }
}
