using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 performance fix: GetListAsync used to load every JournalEntry
// unconditionally (Services/Accounting/JournalEntryAppService.cs) - flagged in a usability/
// performance audit as the first table likely to become visibly slow, since nearly every
// transaction in the app (Invoices, Payments, POS, recurring journals, FX revaluation, bank rec)
// auto-posts to it. Now paged (SkipCount/MaxResultCount) and AccountId-filtered at the query
// level via a correlated Any() subquery against JournalEntryLine.
public class JournalEntryAppServiceTests : ErpTestBase
{
    private async Task<Guid> GetAccountIdAsync(string code)
    {
        var accountRepository = GetRequiredService<IRepository<Account, Guid>>();
        var account = (await accountRepository.GetListAsync(x => x.Code == code)).Single();
        return account.Id;
    }

    private async Task<Guid> CreateBalancedEntryAsync(Guid debitAccountId, Guid creditAccountId, decimal amount, string description)
    {
        var journalEntryAppService = GetRequiredService<JournalEntryAppService>();
        var entry = await journalEntryAppService.CreateAsync(new CreateJournalEntryDto
        {
            EntryDate = DateTime.UtcNow,
            Description = description,
            Lines = new()
            {
                new CreateJournalEntryLineDto { AccountId = debitAccountId, Debit = amount, CurrencyCode = "KES" },
                new CreateJournalEntryLineDto { AccountId = creditAccountId, Credit = amount, CurrencyCode = "KES" }
            }
        });
        return entry.Id;
    }

    [Fact]
    public async Task GetListAsync_Pages_Results_And_Reports_TotalCount_Across_All_Pages()
    {
        await EnsureDatabaseCreatedAsync();
        var cashId = await GetAccountIdAsync("1000");
        var equityId = await GetAccountIdAsync("3000");

        await CreateBalancedEntryAsync(cashId, equityId, 100m, "Entry 1");
        await CreateBalancedEntryAsync(cashId, equityId, 200m, "Entry 2");
        await CreateBalancedEntryAsync(cashId, equityId, 300m, "Entry 3");

        var journalEntryAppService = GetRequiredService<JournalEntryAppService>();

        var firstPage = await journalEntryAppService.GetListAsync(new GetJournalEntryListInput { SkipCount = 0, MaxResultCount = 2 });
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);

        var secondPage = await journalEntryAppService.GetListAsync(new GetJournalEntryListInput { SkipCount = 2, MaxResultCount = 2 });
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Single(secondPage.Items);
    }

    [Fact]
    public async Task GetListAsync_Filters_By_AccountId_Via_Its_Lines()
    {
        await EnsureDatabaseCreatedAsync();
        var cashId = await GetAccountIdAsync("1000");
        var equityId = await GetAccountIdAsync("3000");
        var inventoryId = await GetAccountIdAsync("1200");

        var matchingEntryId = await CreateBalancedEntryAsync(inventoryId, equityId, 500m, "Touches Inventory");
        await CreateBalancedEntryAsync(cashId, equityId, 100m, "Does not touch Inventory");

        var journalEntryAppService = GetRequiredService<JournalEntryAppService>();
        var result = await journalEntryAppService.GetListAsync(new GetJournalEntryListInput { AccountId = inventoryId });

        var item = Assert.Single(result.Items);
        Assert.Equal(matchingEntryId, item.Id);
    }
}
