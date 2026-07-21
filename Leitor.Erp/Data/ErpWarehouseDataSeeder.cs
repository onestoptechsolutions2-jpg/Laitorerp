using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds a single default warehouse on first run only, so single-location shops never have to
// think about warehouses at all - every new Order/GoodsReceipt defaults to whichever warehouse
// has IsDefault set. Same safe-to-rerun convention as ErpTaxRateDataSeeder/ErpCurrencyDataSeeder.
public class ErpWarehouseDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpWarehouseDataSeeder(IRepository<Warehouse, Guid> warehouseRepository, IGuidGenerator guidGenerator)
    {
        _warehouseRepository = warehouseRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _warehouseRepository.GetCountAsync() > 0)
        {
            return;
        }

        await _warehouseRepository.InsertAsync(new Warehouse(_guidGenerator.Create(), "Main Warehouse")
        {
            IsDefault = true
        });
    }
}
