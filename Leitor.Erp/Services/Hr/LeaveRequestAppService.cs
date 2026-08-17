using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Hr;

[RequiresFeature(ErpFeatures.HumanResources)]
public class LeaveRequestAppService :
    CrudAppService<LeaveRequest, LeaveRequestDto, Guid, GetLeaveRequestListInput, CreateUpdateLeaveRequestDto>
{
    // Must match LeaveRequestEscalationHandler.ActionType exactly - both this AppService and the
    // handler reference the same string constant so they can't silently drift apart.
    public const string ApproveActionType = "LeaveRequest.Approve";

    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<EscalationItem, Guid> _escalationItemRepository;

    public LeaveRequestAppService(
        IRepository<LeaveRequest, Guid> repository,
        IRepository<Employee, Guid> employeeRepository,
        IRepository<EscalationItem, Guid> escalationItemRepository)
        : base(repository)
    {
        _employeeRepository = employeeRepository;
        _escalationItemRepository = escalationItemRepository;

        GetPolicyName = ErpPermissions.Leave.Default;
        GetListPolicyName = ErpPermissions.Leave.Default;
        CreatePolicyName = ErpPermissions.Leave.Create;
        UpdatePolicyName = ErpPermissions.Leave.Edit;
        DeletePolicyName = ErpPermissions.Leave.Edit;
    }

    protected override async Task<IQueryable<LeaveRequest>> CreateFilteredQueryAsync(GetLeaveRequestListInput input)
    {
        input.Sorting ??= $"{nameof(LeaveRequest.StartDate)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.EmployeeId.HasValue, x => x.EmployeeId == input.EmployeeId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value);
    }

    public override async Task<LeaveRequestDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveDisplayDataAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<LeaveRequestDto>> GetListAsync(GetLeaveRequestListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveDisplayDataAsync(result.Items);
        return result;
    }

    // Resolves EmployeeName, and reconciles a Rejected decision into the displayed Status -
    // RejectAsync (EscalationItemAppService.cs) only ever flips the EscalationItem's own Status,
    // it doesn't (and shouldn't) know about LeaveRequest specifically, so a rejection is reflected
    // here at read time instead of by making the generic rejection path entity-aware.
    private async Task ResolveDisplayDataAsync(IReadOnlyCollection<LeaveRequestDto> items)
    {
        var employeeIds = items.Select(x => x.EmployeeId).Distinct().ToList();
        if (employeeIds.Count > 0)
        {
            var namesById = (await _employeeRepository.GetListAsync(x => employeeIds.Contains(x.Id)))
                .ToDictionary(x => x.Id, x => x.FullName);
            foreach (var item in items)
            {
                if (namesById.TryGetValue(item.EmployeeId, out var name))
                {
                    item.EmployeeName = name;
                }
            }
        }

        var pendingIds = items.Where(x => x.Status == LeaveRequestStatus.PendingApproval).Select(x => x.Id).ToList();
        if (pendingIds.Count == 0)
        {
            return;
        }

        var escalations = await _escalationItemRepository.GetListAsync(x =>
            x.ActionType == ApproveActionType && pendingIds.Contains(x.EntityId));

        foreach (var item in items)
        {
            var latest = escalations.Where(x => x.EntityId == item.Id).OrderByDescending(x => x.RequestedAt).FirstOrDefault();
            if (latest?.Status == EscalationItemStatus.Rejected)
            {
                item.Status = LeaveRequestStatus.Rejected;
            }
        }
    }

    protected override Task<LeaveRequest> MapToEntityAsync(CreateUpdateLeaveRequestDto createInput)
    {
        var entity = new LeaveRequest(
            GuidGenerator.Create(), createInput.EmployeeId, createInput.LeaveType,
            createInput.StartDate, createInput.EndDate, createInput.DaysRequested)
        {
            Reason = createInput.Reason
        };
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateLeaveRequestDto updateInput, LeaveRequest entity)
    {
        if (entity.Status != LeaveRequestStatus.Draft)
        {
            throw new UserFriendlyException("Only a Draft leave request can be edited - it has already been submitted for approval.");
        }

        entity.EmployeeId = updateInput.EmployeeId;
        entity.LeaveType = updateInput.LeaveType;
        entity.StartDate = updateInput.StartDate;
        entity.EndDate = updateInput.EndDate;
        entity.DaysRequested = updateInput.DaysRequested;
        entity.Reason = updateInput.Reason;
        return Task.CompletedTask;
    }

    // Draft -> PendingApproval, then files the generic EscalationItem - mirrors
    // QuoteAppService.SendAsync's own "dedicated transition entry point" reasoning, so the caller
    // doesn't have to reconstruct a full CreateUpdateLeaveRequestDto just to flip Status.
    // EscalationGate.FileAsync always throws a UserFriendlyException once filed (by design - see
    // its own comment), so the Status flip and save must happen first.
    public virtual async Task SubmitAsync(Guid id)
    {
        await CheckUpdatePolicyAsync();

        var entity = await Repository.GetAsync(id);
        if (entity.Status != LeaveRequestStatus.Draft)
        {
            throw new UserFriendlyException("Only a Draft leave request can be submitted for approval.");
        }

        entity.Status = LeaveRequestStatus.PendingApproval;
        await Repository.UpdateAsync(entity, autoSave: true);

        await EscalationGate.FileAsync(
            _escalationItemRepository,
            CurrentUser,
            GuidGenerator,
            Clock,
            actionType: ApproveActionType,
            requiredPermission: ErpPermissions.Leave.Approve,
            entityType: "LeaveRequest",
            entityId: id,
            payload: null,
            reason: entity.Reason,
            filedMessage: "Leave request submitted for approval.");
    }
}
