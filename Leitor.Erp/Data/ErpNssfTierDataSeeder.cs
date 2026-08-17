using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Hr;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds Kenya's NSSF 2-tier contribution on first run only, same empty-table-only guard as
// ErpTaxRateDataSeeder. Best-effort current figures as of this feature's implementation date
// (2026-08-17): Tier I covers pensionable earnings up to the Lower Earnings Limit (KES 8,000),
// Tier II covers the band up to the Upper Earnings Limit, both at 6% employee + 6% employer. THE
// UPPER EARNINGS LIMIT IS ESPECIALLY LIKELY TO BE STALE: the NSSF Act 2013 phases it in on an
// annual statutory schedule (it has already risen multiple times since Tier II contributions
// began in Feb 2025), so this figure MUST be verified against the current published NSSF rate
// before real payroll use, more urgently than the other seeded payroll figures. Editable
// afterward via the Payroll > Tax Bands admin page.
public class ErpNssfTierDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<NssfTier, Guid> _nssfTierRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpNssfTierDataSeeder(IRepository<NssfTier, Guid> nssfTierRepository, IGuidGenerator guidGenerator)
    {
        _nssfTierRepository = nssfTierRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _nssfTierRepository.GetCountAsync() > 0)
        {
            return;
        }

        var effectiveFrom = new DateTime(2025, 2, 1);

        await _nssfTierRepository.InsertAsync(new NssfTier(_guidGenerator.Create(), 1, 0, 8_000m, 6m, 6m, effectiveFrom));
        await _nssfTierRepository.InsertAsync(new NssfTier(_guidGenerator.Create(), 2, 8_000m, 72_000m, 6m, 6m, effectiveFrom));
    }
}
