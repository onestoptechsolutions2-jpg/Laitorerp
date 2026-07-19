using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Leitor.Erp.Services.Governance;

// Called from every AppService at each business-meaningful moment listed in the workflow
// governance plan (Lead qualified, Opportunity opened, Proposal sent, Order confirmed, deposit
// invoice issued, installation scheduled/completed, final invoice issued, unlock-for-revision,
// etc). Same static-method-with-injected-deps shape as DeletionGate, so callers don't need to
// take a dependency on a stateful service just to append one log row.
public static class WorkflowStageLog
{
    public static async Task RecordAsync(
        IRepository<WorkflowStageEvent, Guid> repository,
        IGuidGenerator guidGenerator,
        ICurrentUser currentUser,
        IClock clock,
        string entityType,
        Guid entityId,
        WorkflowStage stage,
        string? channel = null,
        string? notes = null)
    {
        var entity = new WorkflowStageEvent(guidGenerator.Create(), entityType, entityId, stage, clock.Now)
        {
            UserId = currentUser.Id,
            Channel = channel,
            Notes = notes
        };

        await repository.InsertAsync(entity, autoSave: true);
    }
}
