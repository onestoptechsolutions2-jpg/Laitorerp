using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds a small standard starter Chart of Accounts on first run only - editable/extendable
// afterward, same safe-to-rerun convention as ErpTaxRateDataSeeder/ErpCurrencyDataSeeder. Only
// the accounts JournalPostingService actually needs (via SystemRole) are seeded; everything else
// an org needs is added manually through the Chart of Accounts page.
public class ErpChartOfAccountsDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpChartOfAccountsDataSeeder(IRepository<Account, Guid> accountRepository, IGuidGenerator guidGenerator)
    {
        _accountRepository = accountRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _accountRepository.GetCountAsync() > 0)
        {
            return;
        }

        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "1000", "Cash", AccountType.Asset)
        {
            SystemRole = SystemAccountRole.Cash
        });
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "1100", "Accounts Receivable", AccountType.Asset)
        {
            SystemRole = SystemAccountRole.AccountsReceivable
        });
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "1200", "Inventory", AccountType.Asset)
        {
            SystemRole = SystemAccountRole.Inventory
        });
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "2000", "Accounts Payable", AccountType.Liability)
        {
            SystemRole = SystemAccountRole.AccountsPayable
        });
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "3000", "Owner's Equity", AccountType.Equity));
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "4000", "Revenue", AccountType.Revenue)
        {
            SystemRole = SystemAccountRole.Revenue
        });
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "5000", "Cost of Goods Sold", AccountType.Expense)
        {
            SystemRole = SystemAccountRole.Expense
        });
        await _accountRepository.InsertAsync(new Account(_guidGenerator.Create(), "5100", "Operating Expenses", AccountType.Expense));
    }
}
