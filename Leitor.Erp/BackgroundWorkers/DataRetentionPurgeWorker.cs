using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Leitor.Erp.BackgroundWorkers;

// Runs once daily: hard-deletes rows that were already soft-deleted more than
// DataRetentionOptions.Years ago, for a deliberately conservative list of auxiliary/log-style
// entities (append-only notes, tasks, attachments, messages) rather than core financial records
// (Invoice, Payment, Order, JournalEntry) that stay soft-deleted indefinitely for audit purposes.
// Uses IRepository.DeleteDirectAsync, which issues the delete straight against the database
// without loading entities into the change tracker - the only way to truly remove a row that
// already has IsDeleted=true, since a tracked-entity DeleteAsync would just re-apply the
// soft-delete conversion as a no-op. The ISoftDelete filter is disabled first so the predicate can
// see already-deleted rows at all.
public class DataRetentionPurgeWorker : AsyncPeriodicBackgroundWorkerBase
{
    public DataRetentionPurgeWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var dataFilter = workerContext.ServiceProvider.GetRequiredService<IDataFilter>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var retentionYears = workerContext.ServiceProvider.GetRequiredService<IOptions<DataRetentionOptions>>().Value.Years;

        var cutoff = clock.Now.AddYears(-retentionYears);

        using (dataFilter.Disable<ISoftDelete>())
        {
            await PurgeAsync(workerContext.ServiceProvider.GetRequiredService<IRepository<Lead, Guid>>(), x => x.IsDeleted && x.DeletionTime != null && x.DeletionTime < cutoff);
            await PurgeAsync(workerContext.ServiceProvider.GetRequiredService<IRepository<CustomerNote, Guid>>(), x => x.IsDeleted && x.DeletionTime != null && x.DeletionTime < cutoff);
            await PurgeAsync(workerContext.ServiceProvider.GetRequiredService<IRepository<CustomerTask, Guid>>(), x => x.IsDeleted && x.DeletionTime != null && x.DeletionTime < cutoff);
            await PurgeAsync(workerContext.ServiceProvider.GetRequiredService<IRepository<CustomerAttachment, Guid>>(), x => x.IsDeleted && x.DeletionTime != null && x.DeletionTime < cutoff);
            await PurgeAsync(workerContext.ServiceProvider.GetRequiredService<IRepository<TicketMessage, Guid>>(), x => x.IsDeleted && x.DeletionTime != null && x.DeletionTime < cutoff);
            await PurgeAsync(workerContext.ServiceProvider.GetRequiredService<IRepository<FieldServiceJobNote, Guid>>(), x => x.IsDeleted && x.DeletionTime != null && x.DeletionTime < cutoff);
        }
    }

    // Predicate is built by the caller against the concrete entity type, not a shared interface
    // constraint - IsDeleted/DeletionTime resolve fine there since every FullAuditedAggregateRoot
    // has them concretely, without needing to know which ABP interface declares which member.
    private static Task PurgeAsync<TEntity>(IRepository<TEntity, Guid> repository, Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, IEntity<Guid>
    {
        return repository.DeleteDirectAsync(predicate);
    }
}
