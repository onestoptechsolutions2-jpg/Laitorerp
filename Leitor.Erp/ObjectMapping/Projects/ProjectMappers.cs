using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Services.Dtos.Projects;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Projects;

[Mapper]
public partial class ProjectToProjectDtoMapper : MapperBase<Project, ProjectDto>
{
    [MapperIgnoreSource(nameof(Project.ExtraProperties))]
    [MapperIgnoreSource(nameof(Project.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProjectDto.CustomerName))]
    public override partial ProjectDto Map(Project source);

    [MapperIgnoreSource(nameof(Project.ExtraProperties))]
    [MapperIgnoreSource(nameof(Project.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProjectDto.CustomerName))]
    public override partial void Map(Project source, ProjectDto destination);
}

[Mapper]
public partial class ProjectTaskToProjectTaskDtoMapper : MapperBase<ProjectTask, ProjectTaskDto>
{
    [MapperIgnoreSource(nameof(ProjectTask.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProjectTask.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProjectTaskDto.AssignedToUserName))]
    public override partial ProjectTaskDto Map(ProjectTask source);

    [MapperIgnoreSource(nameof(ProjectTask.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProjectTask.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProjectTaskDto.AssignedToUserName))]
    public override partial void Map(ProjectTask source, ProjectTaskDto destination);
}
