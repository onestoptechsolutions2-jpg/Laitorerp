using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Governance;

// Read/decide surface for EscalationItem. Unlike DeletionRequestAppService (one hardcoded
// permission for every item via a class-level [Authorize] on Approve/Reject), each item's own
// RequiredPermission varies by ActionType, so decide-ability is checked manually via
// CanDecideAsync rather than a static attribute. Every check in this class is therefore a manual
// AuthorizationService.IsGrantedAsync call (no [Authorize] attributes at all, including on
// GetListAsync) - kept consistent on purpose rather than mixing an attribute-driven check for
// "view" with manual checks for "decide". Approving dispatches to whichever
// IEscalationActionHandler is registered for the item's ActionType (see IEscalationActionHandler's
// own comment) instead of a hardcoded entity-type switch.
public class EscalationItemAppService : ApplicationService
{
    private readonly IRepository<EscalationItem, Guid> _repository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IEnumerable<IEscalationActionHandler> _handlers;

    public EscalationItemAppService(
        IRepository<EscalationItem, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IEnumerable<IEscalationActionHandler> handlers)
    {
        _repository = repository;
        _identityUserRepository = identityUserRepository;
        _handlers = handlers;
    }

    public virtual async Task<PagedResultDto<EscalationItemDto>> GetListAsync(GetEscalationItemListInput input)
    {
        if (!await AuthorizationService.IsGrantedAsync(ErpPermissions.Escalations.Default))
        {
            throw new AbpAuthorizationException();
        }

        var queryable = await _repository.GetQueryableAsync();
        queryable = queryable
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.ActionType), x => x.ActionType == input.ActionType);

        var totalCount = queryable.Count();
        var items = queryable.OrderByDescending(x => x.RequestedAt).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        var dtos = items.Select(x => ObjectMapper.Map<EscalationItem, EscalationItemDto>(x)).ToList();
        await ResolveUserNamesAsync(dtos);

        return new PagedResultDto<EscalationItemDto>(totalCount, dtos);
    }

    private async Task ResolveUserNamesAsync(IReadOnlyCollection<EscalationItemDto> items)
    {
        var userIds = items
            .Where(x => x.RequestedByUserId.HasValue)
            .Select(x => x.RequestedByUserId!.Value)
            .Concat(items.Where(x => x.DecidedByUserId.HasValue).Select(x => x.DecidedByUserId!.Value))
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
        var namesById = users.ToDictionary(x => x.Id, x => x.UserName);

        foreach (var item in items)
        {
            if (item.RequestedByUserId.HasValue && namesById.TryGetValue(item.RequestedByUserId.Value, out var requestedByName))
            {
                item.RequestedByUserName = requestedByName;
            }

            if (item.DecidedByUserId.HasValue && namesById.TryGetValue(item.DecidedByUserId.Value, out var decidedByName))
            {
                item.DecidedByUserName = decidedByName;
            }
        }
    }

    // Escalations.Decide is a catch-all for an admin/ops-lead clearing the queue without holding
    // every domain permission; an item's own RequiredPermission (e.g. Sales.OverrideMarginGate)
    // is the normal path. Exposed publicly so the Razor page can compute per-row button visibility
    // without duplicating this OR-of-two-permissions logic.
    public virtual async Task<bool> CanDecideAsync(string requiredPermission)
    {
        return await AuthorizationService.IsGrantedAsync(requiredPermission)
            || await AuthorizationService.IsGrantedAsync(ErpPermissions.Escalations.Decide);
    }

    public virtual async Task ApproveAsync(Guid id)
    {
        var item = await _repository.GetAsync(id);
        if (item.Status != EscalationItemStatus.Pending)
        {
            throw new UserFriendlyException("This escalation has already been decided.");
        }

        if (!await CanDecideAsync(item.RequiredPermission))
        {
            throw new AbpAuthorizationException("You don't have the required permission to decide this escalation.");
        }

        var handler = _handlers.FirstOrDefault(x => x.ActionType == item.ActionType);
        if (handler == null)
        {
            throw new UserFriendlyException($"No handler registered for action type '{item.ActionType}'.");
        }

        try
        {
            await handler.ExecuteAsync(item);
            item.Status = EscalationItemStatus.Approved;
        }
        catch (UserFriendlyException ex)
        {
            // Expected failure mode unique to escalations (unlike DeletionRequest, where the
            // dispatched delete basically can't fail): whatever the handler re-validated no
            // longer holds (e.g. the document's state drifted since filing). Recorded, not
            // rethrown, so the queue shows a clear Failed row instead of a raw error page.
            item.Status = EscalationItemStatus.Failed;
            item.ExecutionError = ex.Message;
        }

        item.DecidedByUserId = CurrentUser.Id;
        item.DecidedAt = Clock.Now;
        await _repository.UpdateAsync(item);
    }

    public virtual async Task RejectAsync(Guid id, string? notes)
    {
        var item = await _repository.GetAsync(id);
        if (item.Status != EscalationItemStatus.Pending)
        {
            throw new UserFriendlyException("This escalation has already been decided.");
        }

        if (!await CanDecideAsync(item.RequiredPermission))
        {
            throw new AbpAuthorizationException("You don't have the required permission to decide this escalation.");
        }

        item.Status = EscalationItemStatus.Rejected;
        item.DecidedByUserId = CurrentUser.Id;
        item.DecidedAt = Clock.Now;
        item.DecisionNotes = notes;
        await _repository.UpdateAsync(item);
    }
}
