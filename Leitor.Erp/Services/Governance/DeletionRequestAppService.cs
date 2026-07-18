using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Procurement;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Governance;

// Read/decide surface for DeletionRequest. Approving dispatches to the same entity-specific
// AppService.DeleteAsync a .Decide holder would've called directly - by the time this runs the
// caller already holds .Decide, so DeletionGate lets the delete proceed instead of filing another
// request. Rejecting only ever flips the request's own status.
[Authorize(ErpPermissions.DeletionApprovals.Default)]
public class DeletionRequestAppService : ApplicationService
{
    private readonly IRepository<DeletionRequest, Guid> _repository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly CustomerAppService _customerAppService;
    private readonly VendorAppService _vendorAppService;
    private readonly OrderAppService _orderAppService;
    private readonly InvoiceAppService _invoiceAppService;
    private readonly TicketAppService _ticketAppService;
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly PurchaseOrderAppService _purchaseOrderAppService;

    public DeletionRequestAppService(
        IRepository<DeletionRequest, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        CustomerAppService customerAppService,
        VendorAppService vendorAppService,
        OrderAppService orderAppService,
        InvoiceAppService invoiceAppService,
        TicketAppService ticketAppService,
        FieldServiceJobAppService fieldServiceJobAppService,
        PurchaseOrderAppService purchaseOrderAppService)
    {
        _repository = repository;
        _identityUserRepository = identityUserRepository;
        _customerAppService = customerAppService;
        _vendorAppService = vendorAppService;
        _orderAppService = orderAppService;
        _invoiceAppService = invoiceAppService;
        _ticketAppService = ticketAppService;
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public virtual async Task<PagedResultDto<DeletionRequestDto>> GetListAsync(GetDeletionRequestListInput input)
    {
        var queryable = await _repository.GetQueryableAsync();
        queryable = queryable.WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value);

        var totalCount = queryable.Count();
        var items = queryable.OrderByDescending(x => x.RequestedAt).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        var dtos = items.Select(x => ObjectMapper.Map<DeletionRequest, DeletionRequestDto>(x)).ToList();
        await ResolveUserNamesAsync(dtos);

        return new PagedResultDto<DeletionRequestDto>(totalCount, dtos);
    }

    private async Task ResolveUserNamesAsync(IReadOnlyCollection<DeletionRequestDto> requests)
    {
        var userIds = requests
            .Where(x => x.RequestedByUserId.HasValue)
            .Select(x => x.RequestedByUserId!.Value)
            .Concat(requests.Where(x => x.DecidedByUserId.HasValue).Select(x => x.DecidedByUserId!.Value))
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
        var namesById = users.ToDictionary(x => x.Id, x => x.UserName);

        foreach (var request in requests)
        {
            if (request.RequestedByUserId.HasValue && namesById.TryGetValue(request.RequestedByUserId.Value, out var requestedByName))
            {
                request.RequestedByUserName = requestedByName;
            }

            if (request.DecidedByUserId.HasValue && namesById.TryGetValue(request.DecidedByUserId.Value, out var decidedByName))
            {
                request.DecidedByUserName = decidedByName;
            }
        }
    }

    [Authorize(ErpPermissions.DeletionApprovals.Decide)]
    public virtual async Task ApproveAsync(Guid id)
    {
        var request = await _repository.GetAsync(id);
        if (request.Status != DeletionRequestStatus.Pending)
        {
            throw new UserFriendlyException("This request has already been decided.");
        }

        await DispatchDeleteAsync(request.EntityType, request.EntityId);

        request.Status = DeletionRequestStatus.Approved;
        request.DecidedByUserId = CurrentUser.Id;
        request.DecidedAt = Clock.Now;
        await _repository.UpdateAsync(request);
    }

    [Authorize(ErpPermissions.DeletionApprovals.Decide)]
    public virtual async Task RejectAsync(Guid id, string? notes)
    {
        var request = await _repository.GetAsync(id);
        if (request.Status != DeletionRequestStatus.Pending)
        {
            throw new UserFriendlyException("This request has already been decided.");
        }

        request.Status = DeletionRequestStatus.Rejected;
        request.DecidedByUserId = CurrentUser.Id;
        request.DecidedAt = Clock.Now;
        request.DecisionNotes = notes;
        await _repository.UpdateAsync(request);
    }

    private Task DispatchDeleteAsync(string entityType, Guid entityId)
    {
        return entityType switch
        {
            "Customer" => _customerAppService.DeleteAsync(entityId),
            "Vendor" => _vendorAppService.DeleteAsync(entityId),
            "Order" => _orderAppService.DeleteAsync(entityId),
            "Invoice" => _invoiceAppService.DeleteAsync(entityId),
            "Ticket" => _ticketAppService.DeleteAsync(entityId),
            "FieldServiceJob" => _fieldServiceJobAppService.DeleteAsync(entityId),
            "PurchaseOrder" => _purchaseOrderAppService.DeleteAsync(entityId),
            _ => throw new UserFriendlyException($"Unknown entity type: {entityType}")
        };
    }
}
