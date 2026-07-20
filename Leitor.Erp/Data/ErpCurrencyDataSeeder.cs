using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds KES as the base/operating currency (matching the existing seeded Kenyan VAT tax rates)
// plus USD as the one other currency in common use here - only runs if the table is empty, same
// safe-to-rerun convention as ErpTaxRateDataSeeder.
public class ErpCurrencyDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpCurrencyDataSeeder(IRepository<Currency, Guid> currencyRepository, IGuidGenerator guidGenerator)
    {
        _currencyRepository = currencyRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _currencyRepository.GetCountAsync() > 0)
        {
            return;
        }

        await _currencyRepository.InsertAsync(new Currency(_guidGenerator.Create(), "KES", "Kenyan Shilling", "KSh")
        {
            IsBaseCurrency = true
        });
        await _currencyRepository.InsertAsync(new Currency(_guidGenerator.Create(), "USD", "US Dollar", "$"));
    }
}
