using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.FieldService;

public class FieldServiceJobAppService :
    CrudAppService<FieldServiceJob, FieldServiceJobDto, Guid, GetFieldServiceJobListInput, CreateUpdateFieldServiceJobDto>
{
    private readonly IRepository<FieldServiceJobNote, Guid> _noteRepository;
    private readonly IRepository<FieldServiceJobPart, Guid> _partRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderPaymentMilestone, Guid> _milestoneRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IClock _clock;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public FieldServiceJobAppService(
        IRepository<FieldServiceJob, Guid> repository,
        IRepository<FieldServiceJobNote, Guid> noteRepository,
        IRepository<FieldServiceJobPart, Guid> partRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderPaymentMilestone, Guid> milestoneRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IClock clock,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _noteRepository = noteRepository;
        _partRepository = partRepository;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _vendorRepository = vendorRepository;
        _orderRepository = orderRepository;
        _milestoneRepository = milestoneRepository;
        _stageEventRepository = stageEventRepository;
        _clock = clock;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.FieldService.Default;
        GetListPolicyName = ErpPermissions.FieldService.Default;
        CreatePolicyName = ErpPermissions.FieldService.Create;
        UpdatePolicyName = ErpPermissions.FieldService.Edit;
        DeletePolicyName = ErpPermissions.FieldService.Delete;
    }

    // Notes and parts are independent aggregate roots (see their entity file comments) with no FK
    // relationship configured, so deleting a job doesn't cascade automatically - same pattern as
    // CustomerAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "FieldServiceJob", id);

        var notes = await _noteRepository.GetListAsync(x => x.JobId == id);
        await _noteRepository.DeleteManyAsync(notes);

        var parts = await _partRepository.GetListAsync(x => x.JobId == id);
        await _partRepository.DeleteManyAsync(parts);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<FieldServiceJob>> CreateFilteredQueryAsync(GetFieldServiceJobListInput input)
    {
        input.Sorting ??= $"{nameof(FieldServiceJob.ScheduledDate)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(input.AssignedToUserId.HasValue, x => x.AssignedToUserId == input.AssignedToUserId!.Value)
            .WhereIf(
                !string.IsNullOrWhiteSpace(input.Filter),
                x => (x.Description != null && x.Description.Contains(input.Filter!)) ||
                     (x.SiteAddress != null && x.SiteAddress.Contains(input.Filter!))
            );
    }

    public override async Task<FieldServiceJobDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<FieldServiceJobDto>> GetListAsync(GetFieldServiceJobListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<FieldServiceJobDto> jobs)
    {
        var customerIds = jobs.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var customerNamesById = customers.ToDictionary(x => x.Id, x => x.Name);

        var userIds = jobs
            .Where(x => x.AssignedToUserId.HasValue)
            .Select(x => x.AssignedToUserId!.Value)
            .Distinct()
            .ToList();
        var usersById = userIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        var vendorIds = jobs
            .Where(x => x.VendorId.HasValue)
            .Select(x => x.VendorId!.Value)
            .Distinct()
            .ToList();
        var vendorNamesById = vendorIds.Count > 0
            ? (await _vendorRepository.GetListAsync(x => vendorIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var job in jobs)
        {
            if (customerNamesById.TryGetValue(job.CustomerId, out var customerName))
            {
                job.CustomerName = customerName;
            }

            if (job.AssignedToUserId.HasValue && usersById.TryGetValue(job.AssignedToUserId.Value, out var userName))
            {
                job.AssignedToUserName = userName;
            }

            if (job.VendorId.HasValue && vendorNamesById.TryGetValue(job.VendorId.Value, out var vendorName))
            {
                job.VendorName = vendorName;
            }
        }
    }

    // CreateUpdateFieldServiceJobDto -> FieldServiceJob is mapped manually rather than via
    // Mapperly - same reason as every other entity in this app (protected Id setter).
    protected override async Task<FieldServiceJob> MapToEntityAsync(CreateUpdateFieldServiceJobDto createInput)
    {
        if (createInput.OrderId.HasValue)
        {
            await EnsureInstallationGateAsync(createInput.OrderId.Value);
        }

        var entity = new FieldServiceJob(GuidGenerator.Create(), createInput.CustomerId);
        await CopyToEntityAsync(createInput, entity);

        if (createInput.OrderId.HasValue)
        {
            await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", createInput.OrderId.Value, WorkflowStage.InstallationScheduled);
        }

        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateFieldServiceJobDto updateInput, FieldServiceJob entity)
    {
        await CopyToEntityAsync(updateInput, entity);
    }

    // A job billed against a Sales Order can't be scheduled until that order's earned the right to
    // be worked on: for a Milestone-terms order that means the Deposit has actually been invoiced;
    // for any other order it just means the order has been confirmed (no separate deposit concept
    // applies - see OrderAppService.OnOrderConfirmedAsync's "deliberate scope cut").
    private async Task EnsureInstallationGateAsync(Guid orderId)
    {
        var order = await _orderRepository.GetAsync(orderId);

        if (order.PaymentTerms == PaymentTerms.Milestone)
        {
            var depositInvoiced = (await _milestoneRepository.GetListAsync(x =>
                x.OrderId == orderId && x.Kind == OrderPaymentMilestoneKind.Deposit && x.IsInvoiced)).Any();

            if (!depositInvoiced)
            {
                throw new UserFriendlyException("This order's deposit invoice hasn't been issued yet - installation can't be scheduled until it has.");
            }
        }
        else if (order.Status == OrderStatus.Submitted)
        {
            throw new UserFriendlyException("This order hasn't been confirmed yet - installation can't be scheduled until it has.");
        }
    }

    private async Task CopyToEntityAsync(CreateUpdateFieldServiceJobDto input, FieldServiceJob entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.OrderId = input.OrderId;
        entity.ContractId = input.ContractId;
        entity.Type = input.Type;
        entity.ScheduledDate = input.ScheduledDate;
        entity.AssignedToUserId = input.AssignedToUserId;
        entity.VendorId = input.VendorId;
        entity.SiteAddress = input.SiteAddress;
        entity.Description = input.Description;

        // Completed and Incomplete are both terminal "visit concluded" outcomes (see
        // FieldServiceJobStatus.cs) - CompletedDate tracks the transition into either, cleared if
        // reopened to Scheduled/InProgress, same auto-tracking pattern as CustomerTask.CompletedAt.
        var wasTerminal = entity.Status is FieldServiceJobStatus.Completed or FieldServiceJobStatus.Incomplete;
        var isTerminal = input.Status is FieldServiceJobStatus.Completed or FieldServiceJobStatus.Incomplete;

        if (isTerminal && !wasTerminal)
        {
            entity.CompletedDate = _clock.Now;
        }
        else if (!isTerminal)
        {
            entity.CompletedDate = null;
        }

        if (input.Status == FieldServiceJobStatus.Completed && entity.Status != FieldServiceJobStatus.Completed && entity.OrderId.HasValue)
        {
            await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", entity.OrderId.Value, WorkflowStage.InstallationCompleted);
        }

        entity.Status = input.Status;
    }
}
