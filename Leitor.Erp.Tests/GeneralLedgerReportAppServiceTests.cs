using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Accounting;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace Leitor.Erp.Tests;

// Regression for a live bug found while functionally testing the app: Trial Balance/Balance
// Sheet/Cash Flow/Budget Variance all defaulted their "as of"/"to" date to a date-only value
// (e.g. DateTime.Today), then compared it against JournalEntry.EntryDate with a plain `<=`.
// EntryDate carries a real time-of-day for anything posted via Clock.Now (every POS sale, for
// example) - a same-day entry posted at, say, 2pm always failed that midnight comparison and
// silently vanished from "as of today" reports until the next calendar day.
public class GeneralLedgerReportAppServiceTests : ErpTestBase
{
    [Fact]
    public async Task GetTrialBalanceAsync_Includes_Entries_Posted_Later_The_Same_Day()
    {
        await EnsureDatabaseCreatedAsync();

        var accountRepository = GetRequiredService<IRepository<Account, Guid>>();
        var journalEntryRepository = GetRequiredService<IRepository<JournalEntry, Guid>>();
        var journalEntryLineRepository = GetRequiredService<IRepository<JournalEntryLine, Guid>>();
        var fiscalPeriodRepository = GetRequiredService<IRepository<FiscalPeriod, Guid>>();
        var guidGenerator = GetRequiredService<IGuidGenerator>();
        var dataFilter = GetRequiredService<IDataFilter>();
        var reportAppService = GetRequiredService<GeneralLedgerReportAppService>();

        // Same calendar day as "now", but with a real time-of-day - exactly what Clock.Now
        // produces for a same-day POS sale, as opposed to a date-only form input.
        var sameDayWithTime = DateTime.Today.AddHours(14);

        await JournalPostingService.PostAsync(
            accountRepository, journalEntryRepository, journalEntryLineRepository, fiscalPeriodRepository,
            guidGenerator, dataFilter,
            sameDayWithTime, JournalPostingService.SourceDocumentTypes.PosSale, guidGenerator.Create(),
            "QA regression - same-day POS sale",
            SystemAccountRole.Cash, SystemAccountRole.Revenue,
            1000m, "KES", 1m);

        var report = await reportAppService.GetTrialBalanceAsync(DateTime.Today);

        Assert.Contains(report.Lines, x => x.AccountName == "Cash");
        var cashLine = report.Lines.Single(x => x.AccountName == "Cash");
        Assert.Equal(1000m, cashLine.DebitTotal);
    }
}
