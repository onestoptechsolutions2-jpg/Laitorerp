using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Accounting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Leitor.Erp.BackgroundWorkers;

// Runs once daily: for each active RecurringJournalTemplate whose NextRunDate has arrived, posts a
// real JournalEntry from its lines and advances NextRunDate by Frequency. Posts directly via
// JournalPostingService.PostMultiLineAsync rather than JournalEntryAppService.CreateAsync - a
// background worker has no authenticated CurrentUser, so CreateAsync's CheckPolicyAsync call would
// always throw (same reason ContractExpiryAlertWorker/DataRetentionPurgeWorker never call
// AppServices either). A template whose period is locked, or whose lines no longer balance (e.g. an
// Account was deleted), is skipped and logged rather than crashing the whole run - it's picked up
// again tomorrow once the underlying issue is fixed.
public class RecurringJournalWorker : AsyncPeriodicBackgroundWorkerBase
{
    public RecurringJournalWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var templateRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<RecurringJournalTemplate, Guid>>();
        var lineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<RecurringJournalTemplateLine, Guid>>();
        var journalEntryRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<JournalEntry, Guid>>();
        var journalEntryLineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<JournalEntryLine, Guid>>();
        var fiscalPeriodRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<FiscalPeriod, Guid>>();
        var currencyRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Currency, Guid>>();
        var exchangeRateRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<ExchangeRate, Guid>>();
        var guidGenerator = workerContext.ServiceProvider.GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var dataFilter = workerContext.ServiceProvider.GetRequiredService<IDataFilter>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var logger = workerContext.ServiceProvider.GetRequiredService<ILogger<RecurringJournalWorker>>();

        var now = clock.Now;
        var dueTemplates = await templateRepository.GetListAsync(x => x.IsActive && x.NextRunDate <= now);

        foreach (var template in dueTemplates)
        {
            var templateLines = await lineRepository.GetListAsync(x => x.RecurringJournalTemplateId == template.Id);
            if (templateLines.Count < 2)
            {
                continue;
            }

            try
            {
                var multiLines = new List<JournalPostingService.MultiLineEntry>();
                foreach (var line in templateLines)
                {
                    var rate = await CurrencyRateResolver.ResolveAsync(currencyRepository, exchangeRateRepository, line.CurrencyCode, template.NextRunDate);
                    multiLines.Add(new JournalPostingService.MultiLineEntry(line.AccountId, line.Debit, line.Credit, line.CurrencyCode, rate));
                }

                await JournalPostingService.PostMultiLineAsync(
                    journalEntryRepository, journalEntryLineRepository, fiscalPeriodRepository, guidGenerator, dataFilter,
                    template.NextRunDate, "RecurringJournalTemplate", template.Id,
                    template.Description, multiLines);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Recurring journal template {TemplateId} ({Description}) could not be posted for {NextRunDate} - will retry tomorrow.",
                    template.Id, template.Description, template.NextRunDate);
                continue;
            }

            template.NextRunDate = template.Frequency switch
            {
                RecurringJournalFrequency.Monthly => template.NextRunDate.AddMonths(1),
                RecurringJournalFrequency.Quarterly => template.NextRunDate.AddMonths(3),
                RecurringJournalFrequency.Annually => template.NextRunDate.AddYears(1),
                _ => template.NextRunDate.AddMonths(1)
            };
            await templateRepository.UpdateAsync(template, autoSave: true);
        }
    }
}
