using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Same static-method-with-injected-repository shape as DeletionGate/WorkflowStageLog, called from
// both posting paths a JournalEntry can go through: JournalEntryAppService.CreateAsync (manual)
// and JournalPostingService.PostByAccountIdAsync (auto-posted from Invoices/Payments/etc./
// DepreciationAppService). A month with no FiscalPeriod row is implicitly unlocked.
public static class FiscalPeriodGuard
{
    public static async Task EnsureNotLockedAsync(IRepository<FiscalPeriod, Guid> fiscalPeriodRepository, DateTime entryDate)
    {
        var period = (await fiscalPeriodRepository.GetListAsync(x => x.Year == entryDate.Year && x.Month == entryDate.Month))
            .FirstOrDefault();

        if (period is { IsLocked: true })
        {
            throw new UserFriendlyException(
                $"{entryDate:MMMM yyyy} is a closed accounting period. Unlock it on the Fiscal Periods page first if this entry is really needed.");
        }
    }
}
