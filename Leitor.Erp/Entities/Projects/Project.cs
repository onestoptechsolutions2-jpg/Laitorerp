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

    // Set once when the project is converted into a recurring CustomerContract via the "Convert
    // to Contract" link on Project Detail - records the "projects feed the recurring contract"
    // flywheel from Laitor's stated business model as a real relationship, not just a navigation
    // hint. A loose reference (no FK), same convention as Ticket.ContractId.
    public Guid? ConvertedToContractId { get; set; }

    // One field, two jobs: (a) sequencing - e.g. a CCTV/video-intercom project that needs network
    // infrastructure in place first sets this to that network project, and
    // ProjectDependencyGuard blocks moving Status to Active until the dependency is Completed;
    // (b) lineage - a follow-up/maintenance project created from a completed one's Detail page
    // ("Create Follow-up Project") sets this to the original, same "prefilled Create page + loose
    // Guid back-reference" mechanism as ConvertedToContractId above. Both are really the same
    // relationship shape (this project comes after that one), just initiated from different
    // points in the UI.
    public Guid? DependsOnProjectId { get; set; }

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
