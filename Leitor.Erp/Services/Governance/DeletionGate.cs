using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Leitor.Erp.Services.Governance;

// Deletion is permission-based but gated by approval: everyone gets Delete on the module they
// work in, but only a holder of ErpPermissions.DeletionApprovals.Decide (Admin/Ops Manager) can
// actually remove a scoped record. Everyone else's delete action files a DeletionRequest instead
// and the operation stops there - it never reaches the entity's own cascade-delete logic.
public static class DeletionGate
{
    public static async Task EnsureImmediateDeleteAllowedAsync(
        IAuthorizationService authorizationService,
        ICurrentUser currentUser,
        IRepository<DeletionRequest, Guid> requestRepository,
        IGuidGenerator guidGenerator,
        IClock clock,
        string entityType,
        Guid entityId)
    {
        if (await authorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide))
        {
            return;
        }

        var alreadyPending = (await requestRepository.GetListAsync(x =>
            x.EntityType == entityType && x.EntityId == entityId && x.Status == DeletionRequestStatus.Pending)).Any();

        if (alreadyPending)
        {
            throw new UserFriendlyException("A deletion request for this record is already pending approval.");
        }

        var request = new DeletionRequest(guidGenerator.Create(), entityType, entityId, currentUser.Id, clock.Now);
        await requestRepository.InsertAsync(request, autoSave: true);

        throw new UserFriendlyException("Deletion requires approval - a request has been filed and an administrator will review it.");
    }

    // Used by Detail/Edit pages to show a "pending approval" banner - same (EntityType, EntityId)
    // lookup EnsureImmediateDeleteAllowedAsync uses to avoid filing a duplicate request.
    public static async Task<bool> IsPendingAsync(
        IRepository<DeletionRequest, Guid> requestRepository,
        string entityType,
        Guid entityId)
    {
        return (await requestRepository.GetListAsync(x =>
            x.EntityType == entityType && x.EntityId == entityId && x.Status == DeletionRequestStatus.Pending)).Any();
    }
}
