using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Leitor.Erp.Services.Governance;

// Generalizes DeletionGate for actions that need parameters and vary in which permission gates
// them (DeletionGate hardcodes one permission and has no payload - see EscalationItem.cs).
// Callers are expected to run their own IsGrantedAsync check first (e.g. the margin gate has an
// extra precondition - a reason must be supplied at all - before deciding whether to escalate),
// so unlike DeletionGate.EnsureImmediateDeleteAllowedAsync this doesn't duplicate that check.
public static class EscalationGate
{
    public static async Task FileAsync(
        IRepository<EscalationItem, Guid> repository,
        ICurrentUser currentUser,
        IGuidGenerator guidGenerator,
        IClock clock,
        string actionType,
        string requiredPermission,
        string entityType,
        Guid entityId,
        object? payload,
        string? reason,
        string filedMessage)
    {
        var alreadyPending = (await repository.GetListAsync(x =>
            x.ActionType == actionType && x.EntityId == entityId && x.Status == EscalationItemStatus.Pending)).Any();

        if (alreadyPending)
        {
            throw new UserFriendlyException("An escalation for this action is already pending approval.");
        }

        var item = new EscalationItem(
            guidGenerator.Create(), actionType, entityType, entityId, requiredPermission,
            payload == null ? null : JsonSerializer.Serialize(payload),
            currentUser.Id, clock.Now, reason);

        await repository.InsertAsync(item, autoSave: true);

        throw new UserFriendlyException(filedMessage);
    }

    // Used by Detail/Edit pages to show a "pending approval" banner - same (ActionType, EntityId)
    // lookup FileAsync uses to avoid filing a duplicate request.
    public static async Task<bool> IsPendingAsync(
        IRepository<EscalationItem, Guid> repository,
        string actionType,
        Guid entityId)
    {
        return (await repository.GetListAsync(x =>
            x.ActionType == actionType && x.EntityId == entityId && x.Status == EscalationItemStatus.Pending)).Any();
    }
}
