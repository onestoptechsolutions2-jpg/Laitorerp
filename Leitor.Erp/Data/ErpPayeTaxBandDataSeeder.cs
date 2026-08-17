using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Hr;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds Kenya's PAYE (income tax) bands on first run only, same empty-table-only guard as
// ErpTaxRateDataSeeder - never overwrites an admin's later edits. Best-effort current figures as
// of this feature's implementation date (2026-08-17), monthly bands per the Income Tax Act as
// amended by the Finance Act 2023 (effective 1 July 2023): 10% to KES 24,000, 25% to KES 32,333,
// 30% to KES 500,000, 32.5% to KES 800,000, 35% above. VERIFY against the current published KRA
// PAYE table before this is used for a real payroll run - Finance Act amendments change these
// periodically and this seeder's figures may already be stale by the time it runs. Editable
// afterward via the Payroll > Tax Bands admin page (Pages/Hr/Payroll/TaxBands/Index.cshtml).
public class ErpPayeTaxBandDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<PayeTaxBand, Guid> _payeTaxBandRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpPayeTaxBandDataSeeder(IRepository<PayeTaxBand, Guid> payeTaxBandRepository, IGuidGenerator guidGenerator)
    {
        _payeTaxBandRepository = payeTaxBandRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _payeTaxBandRepository.GetCountAsync() > 0)
        {
            return;
        }

        var effectiveFrom = new DateTime(2023, 7, 1);

        await _payeTaxBandRepository.InsertAsync(new PayeTaxBand(_guidGenerator.Create(), 0, 24_000m, 10m, effectiveFrom));
        await _payeTaxBandRepository.InsertAsync(new PayeTaxBand(_guidGenerator.Create(), 24_000m, 32_333m, 25m, effectiveFrom));
        await _payeTaxBandRepository.InsertAsync(new PayeTaxBand(_guidGenerator.Create(), 32_333m, 500_000m, 30m, effectiveFrom));
        await _payeTaxBandRepository.InsertAsync(new PayeTaxBand(_guidGenerator.Create(), 500_000m, 800_000m, 32.5m, effectiveFrom));
        await _payeTaxBandRepository.InsertAsync(new PayeTaxBand(_guidGenerator.Create(), 800_000m, null, 35m, effectiveFrom));
    }
}
