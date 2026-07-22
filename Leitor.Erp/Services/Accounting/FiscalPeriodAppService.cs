using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// A month with no FiscalPeriod row is implicitly unlocked (see FiscalPeriodGuard) - rows are only
// created the first time a month is actually locked, not pre-seeded for every month that exists.
public class FiscalPeriodAppService : ApplicationService
{
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;

    public FiscalPeriodAppService(IRepository<FiscalPeriod, Guid> fiscalPeriodRepository)
    {
        _fiscalPeriodRepository = fiscalPeriodRepository;
    }

    public async Task<FiscalPeriodGridDto> GetGridAsync(int year)
    {
        await CheckPolicyAsync(ErpPermissions.FiscalPeriods.Default);

        var periods = (await _fiscalPeriodRepository.GetListAsync(x => x.Year == year)).ToDictionary(x => x.Month);

        var grid = new FiscalPeriodGridDto { Year = year };
        for (var month = 1; month <= 12; month++)
        {
            var row = new FiscalPeriodRowDto { Month = month };
            if (periods.TryGetValue(month, out var period))
            {
                row.IsLocked = period.IsLocked;
                row.LockedDate = period.LockedDate;
            }

            grid.Months.Add(row);
        }

        return grid;
    }

    public async Task ToggleAsync(int year, int month, bool locked)
    {
        await CheckPolicyAsync(ErpPermissions.FiscalPeriods.Manage);

        var period = (await _fiscalPeriodRepository.GetListAsync(x => x.Year == year && x.Month == month)).FirstOrDefault();
        if (period == null)
        {
            period = new FiscalPeriod(GuidGenerator.Create(), year, month);
            period.IsLocked = locked;
            period.LockedDate = locked ? Clock.Now : null;
            await _fiscalPeriodRepository.InsertAsync(period, autoSave: true);
        }
        else
        {
            period.IsLocked = locked;
            period.LockedDate = locked ? Clock.Now : null;
            await _fiscalPeriodRepository.UpdateAsync(period, autoSave: true);
        }
    }

    public async Task CloseYearAsync(int year)
    {
        await CheckPolicyAsync(ErpPermissions.FiscalPeriods.Manage);

        for (var month = 1; month <= 12; month++)
        {
            await ToggleAsync(year, month, true);
        }
    }
}
