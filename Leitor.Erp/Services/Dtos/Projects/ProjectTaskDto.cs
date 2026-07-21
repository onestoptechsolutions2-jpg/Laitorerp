using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Projects;

public class ProjectTaskDto : FullAuditedEntityDto<Guid>
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Resolved by ProjectTaskAppService - not a stored column.
    public string? AssignedToUserName { get; set; }
}
