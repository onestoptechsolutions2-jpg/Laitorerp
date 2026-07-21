using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Projects;

// Mirrors CustomerTask exactly (same fields, same IsCompleted/CompletedAt auto-tracking) - a
// lightweight task list, not a full Gantt/dependency scheduler.
public class ProjectTask : FullAuditedAggregateRoot<Guid>
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    protected ProjectTask()
    {
    }

    public ProjectTask(Guid id, Guid projectId, string title)
        : base(id)
    {
        ProjectId = projectId;
        Title = title;
    }
}
