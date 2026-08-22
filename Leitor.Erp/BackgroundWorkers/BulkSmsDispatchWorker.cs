using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sms;
using Leitor.Erp.Services.Sms;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Leitor.Erp.BackgroundWorkers;

// Sends one Queued BulkSmsMessage per tick (~10/minute, matching httpSMS's own free-tier default
// "Messages Per Minute" throttle on the gateway phone) so a bulk send never blocks the admin's
// request (see BulkSmsAppService.QueueBatchAsync, which only inserts Queued rows) and never
// outruns what the single Android handset can actually dispatch.
public class BulkSmsDispatchWorker : AsyncPeriodicBackgroundWorkerBase
{
    public BulkSmsDispatchWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromSeconds(6).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var httpSmsClient = workerContext.ServiceProvider.GetRequiredService<IHttpSmsClient>();
        if (!await httpSmsClient.IsConfiguredAsync())
        {
            return;
        }

        var messageRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<BulkSmsMessage, Guid>>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();

        var queued = (await messageRepository.GetListAsync(x => x.Status == BulkSmsMessageStatus.Queued))
            .OrderBy(x => x.CreationTime)
            .FirstOrDefault();
        if (queued == null)
        {
            return;
        }

        queued.Status = BulkSmsMessageStatus.Sending;
        await messageRepository.UpdateAsync(queued);

        var result = await httpSmsClient.SendAsync(queued.ToPhoneNumber, queued.Content);

        queued.Status = result.Success ? BulkSmsMessageStatus.Sent : BulkSmsMessageStatus.Failed;
        queued.ProviderMessageId = result.ProviderMessageId;
        queued.ErrorMessage = result.ErrorMessage;
        queued.SentAt = clock.Now;
        await messageRepository.UpdateAsync(queued);
    }
}
