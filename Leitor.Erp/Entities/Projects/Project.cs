using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Projects;

// Project-based accounting's cost object: JournalEntryLine.ProjectId tags GL activity against a
// Project (see JournalPostingService.PostAsync's optional projectId parameter), so
// ProjectReportAppService.GetProjectPnLAsync can sum a project's own P&L for near-zero extra cost
// on top of the GL that already exists - same "compute, never store" discipline as every other GL
// report. Order.ProjectId lets an existing Sales Order be attributed to a project so its
// Invoice/Payment postings flow through automatically once tagged.
public class Project : FullAuditedAggregateRoot<Guid>
{
    public string ProjectNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }

    protected Project()
    {
    }

    public Project(Guid id, string projectNumber, Guid customerId, string title)
        : base(id)
    {
        ProjectNumber = projectNumber;
        CustomerId = customerId;
        Title = title;
    }
}
