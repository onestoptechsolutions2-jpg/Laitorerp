using System;
using Leitor.Erp.Entities.Projects;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Projects;

public class ProjectDto : FullAuditedEntityDto<Guid>
{
    public string ProjectNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }

    // Resolved by ProjectAppService - not a stored column.
    public string? CustomerName { get; set; }
}
