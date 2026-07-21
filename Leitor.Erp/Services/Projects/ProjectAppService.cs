using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Projects;

[RequiresFeature(ErpFeatures.ProjectManagement)]
public class ProjectAppService :
    CrudAppService<Project, ProjectDto, Guid, GetProjectListInput, CreateUpdateProjectDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IDataFilter _dataFilter;

    public ProjectAppService(
        IRepository<Project, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<ProjectTask, Guid> taskRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _taskRepository = taskRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Projects.Default;
        GetListPolicyName = ErpPermissions.Projects.Default;
        CreatePolicyName = ErpPermissions.Projects.Create;
        UpdatePolicyName = ErpPermissions.Projects.Edit;
        DeletePolicyName = ErpPermissions.Projects.Delete;
    }

    // Tasks are an independent aggregate root (see ProjectTask.cs) with no FK relationship
    // configured, so deleting a project doesn't cascade automatically - same pattern as
    // TicketAppService.DeleteAsync/CustomerAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Project", id);

        var tasks = await _taskRepository.GetListAsync(x => x.ProjectId == id);
        await _taskRepository.DeleteManyAsync(tasks);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Project>> CreateFilteredQueryAsync(GetProjectListInput input)
    {
        input.Sorting ??= $"{nameof(Project.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Title.Contains(input.Filter!) || x.ProjectNumber.Contains(input.Filter!));
    }

    public override async Task<ProjectDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ProjectDto> projects)
    {
        var customerIds = projects.Select(x => x.CustomerId).Distinct().ToList();
        var namesById = customerIds.Count > 0
            ? (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var project in projects)
        {
            if (namesById.TryGetValue(project.CustomerId, out var customerName))
            {
                project.CustomerName = customerName;
            }
        }
    }

    protected override async Task<Project> MapToEntityAsync(CreateUpdateProjectDto createInput)
    {
        var projectNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "PRJ-");

        var entity = new Project(GuidGenerator.Create(), projectNumber, createInput.CustomerId, createInput.Title);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateProjectDto updateInput, Project entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateProjectDto input, Project entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.Title = input.Title;
        entity.Description = input.Description;
        entity.Status = input.Status;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Budget = input.Budget;
    }
}
