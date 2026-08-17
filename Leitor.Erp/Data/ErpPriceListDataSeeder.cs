using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds one "Standard" PriceList on first run only, same safe-to-rerun convention as
// ErpChartOfAccountsDataSeeder/ErpServiceCatalogDataSeeder - so there's always a valid pick once
// Customer.DefaultPriceListId became required (2026-08-17). Editable/renameable afterward via
// Pages/Catalog/PriceLists; admins can add more price lists any time, this just guarantees the
// dropdown is never empty on a fresh install.
public class ErpPriceListDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<PriceList, Guid> _priceListRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpPriceListDataSeeder(IRepository<PriceList, Guid> priceListRepository, IGuidGenerator guidGenerator)
    {
        _priceListRepository = priceListRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _priceListRepository.GetCountAsync() > 0)
        {
            return;
        }

        await _priceListRepository.InsertAsync(new PriceList(_guidGenerator.Create(), "Standard"));
    }
}
