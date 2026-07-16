using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
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
    private readonly IClock _clock;

    public FieldServiceJobAppService(
        IRepository<FieldServiceJob, Guid> repository,
        IRepository<FieldServiceJobNote, Guid> noteRepository,
        IRepository<FieldServiceJobPart, Guid> partRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IClock clock)
        : base(repository)
    {
        _noteRepository = noteRepository;
        _partRepository = partRepository;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _clock = clock;

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
        }
    }

    // CreateUpdateFieldServiceJobDto -> FieldServiceJob is mapped manually rather than via
    // Mapperly - same reason as every other entity in this app (protected Id setter).
    protected override Task<FieldServiceJob> MapToEntityAsync(CreateUpdateFieldServiceJobDto createInput)
    {
        var entity = new FieldServiceJob(GuidGenerator.Create(), createInput.CustomerId);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateFieldServiceJobDto updateInput, FieldServiceJob entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateFieldServiceJobDto input, FieldServiceJob entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.OrderId = input.OrderId;
        entity.ContractId = input.ContractId;
        entity.Type = input.Type;
        entity.ScheduledDate = input.ScheduledDate;
        entity.AssignedToUserId = input.AssignedToUserId;
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

        entity.Status = input.Status;
    }
}
