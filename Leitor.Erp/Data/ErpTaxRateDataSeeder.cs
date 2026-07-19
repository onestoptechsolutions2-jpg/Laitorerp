using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds the starting Kenyan VAT categories so Products/Quote lines/etc have something to default
// to on first run - only runs if the table is empty, so re-running on every deploy is safe and it
// never fights with rates an admin has since edited or added.
public class ErpTaxRateDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpTaxRateDataSeeder(IRepository<TaxRate, Guid> taxRateRepository, IGuidGenerator guidGenerator)
    {
        _taxRateRepository = taxRateRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _taxRateRepository.GetCountAsync() > 0)
        {
            return;
        }

        await _taxRateRepository.InsertAsync(new TaxRate(_guidGenerator.Create(), "VAT 16%", 16)
        {
            IsDefault = true
        });
        await _taxRateRepository.InsertAsync(new TaxRate(_guidGenerator.Create(), "Zero-Rated", 0));
        await _taxRateRepository.InsertAsync(new TaxRate(_guidGenerator.Create(), "Exempt", 0));
    }
}
