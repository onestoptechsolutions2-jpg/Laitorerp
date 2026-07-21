using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Projects;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Projects;

// Mirrors CustomerTaskAppService exactly - managed inline on Project Detail, no dedicated
// top-level pages of its own.
[RequiresFeature(ErpFeatures.ProjectManagement)]
public class ProjectTaskAppService :
    CrudAppService<ProjectTask, ProjectTaskDto, Guid, GetProjectTaskListInput, CreateUpdateProjectTaskDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IClock _clock;

    public ProjectTaskAppService(
        IRepository<ProjectTask, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IClock clock)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;
        _clock = clock;

        GetPolicyName = ErpPermissions.Projects.Default;
        GetListPolicyName = ErpPermissions.Projects.Default;
        CreatePolicyName = ErpPermissions.Projects.Edit;
        UpdatePolicyName = ErpPermissions.Projects.Edit;
        DeletePolicyName = ErpPermissions.Projects.Edit;
    }

    protected override async Task<IQueryable<ProjectTask>> CreateFilteredQueryAsync(GetProjectTaskListInput input)
    {
        input.Sorting ??= $"{nameof(ProjectTask.IsCompleted)} ASC, {nameof(ProjectTask.DueDate)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.ProjectId.HasValue, x => x.ProjectId == input.ProjectId!.Value);
    }

    public override async Task<PagedResultDto<ProjectTaskDto>> GetListAsync(GetProjectTaskListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveAssigneeNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveAssigneeNamesAsync(IReadOnlyCollection<ProjectTaskDto> tasks)
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

    protected override Task<ProjectTask> MapToEntityAsync(CreateUpdateProjectTaskDto createInput)
    {
        var entity = new ProjectTask(GuidGenerator.Create(), createInput.ProjectId, createInput.Title);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateProjectTaskDto updateInput, ProjectTask entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateProjectTaskDto input, ProjectTask entity)
    {
        entity.ProjectId = input.ProjectId;
        entity.Title = input.Title;
        entity.Description = input.Description;
        entity.DueDate = input.DueDate;
        entity.AssignedToUserId = input.AssignedToUserId;

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
