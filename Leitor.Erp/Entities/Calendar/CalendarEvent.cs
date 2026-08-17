using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Calendar;

// A standalone calendar entry not tied to any other record ("team meeting", "site visit") - the
// editable half of the Shared Team Calendar. The read-only half (FieldServiceJob.ScheduledDate,
// Ticket.SlaDueDate, ProjectTask.DueDate) is never duplicated here; CalendarEventAppService.
// GetFeedAsync merges this table with those at read time instead. Same loose-Guid, no-real-FK
// convention as every other entity in this app.
public class CalendarEvent : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Internal attendee/owner.
    public Guid? AssignedToUserId { get; set; }

    // External attendee, same convention as ProjectTask.AgentId.
    public Guid? AgentId { get; set; }

    protected CalendarEvent()
    {
    }

    public CalendarEvent(Guid id, string title, DateTime startDate)
        : base(id)
    {
        Title = title;
        StartDate = startDate;
    }
}
