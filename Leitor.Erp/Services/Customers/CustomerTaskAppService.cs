using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Customers;

public class CustomerTaskAppService :
    CrudAppService<CustomerTask, CustomerTaskDto, Guid, GetCustomerTaskListInput, CreateUpdateCustomerTaskDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IClock _clock;

    public CustomerTaskAppService(
        IRepository<CustomerTask, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IClock clock)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;
        _clock = clock;

        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Edit;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Edit;
    }

    protected override async Task<IQueryable<CustomerTask>> CreateFilteredQueryAsync(GetCustomerTaskListInput input)
    {
        // Open tasks first (IsCompleted false sorts before true), earliest due date within each group.
        input.Sorting ??= $"{nameof(CustomerTask.IsCompleted)} ASC, {nameof(CustomerTask.DueDate)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value);
    }

    public override async Task<PagedResultDto<CustomerTaskDto>> GetListAsync(GetCustomerTaskListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveAssigneeNamesAsync(result.Items);
        return result;
    }

    public override async Task<CustomerTaskDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveAssigneeNamesAsync(new[] { dto });
        return dto;
    }

    private async Task ResolveAssigneeNamesAsync(System.Collections.Generic.IReadOnlyCollection<CustomerTaskDto> tasks)
    {
        var userIds = tasks
            .Where(x => x.AssignedToUserId.HasValue)
            .Select(x => x.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
        var namesById = users.ToDictionary(x => x.Id, x => x.UserName);

        foreach (var task in tasks)
        {
            if (task.AssignedToUserId.HasValue && namesById.TryGetValue(task.AssignedToUserId.Value, out var userName))
            {
                task.AssignedToUserName = userName;
            }
        }
    }

    protected override Task<CustomerTask> MapToEntityAsync(CreateUpdateCustomerTaskDto createInput)
    {
        var entity = new CustomerTask(GuidGenerator.Create(), createInput.CustomerId, createInput.Title);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateCustomerTaskDto updateInput, CustomerTask entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateCustomerTaskDto input, CustomerTask entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.Title = input.Title;
        entity.Description = input.Description;
        entity.DueDate = input.DueDate;
        entity.AssignedToUserId = input.AssignedToUserId;

        // CompletedAt tracks the transition, not just the flag - set once when first marked
        // complete, cleared if reopened, rather than exposing it as a separate endpoint.
        if (input.IsCompleted && !entity.IsCompleted)
        {
            entity.CompletedAt = _clock.Now;
        }
        else if (!input.IsCompleted)
        {
            entity.CompletedAt = null;
        }

        entity.IsCompleted = input.IsCompleted;
    }
}
