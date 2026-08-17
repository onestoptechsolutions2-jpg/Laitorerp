using System;

namespace Leitor.Erp.Services.Dtos.Calendar;

// One row on the merged calendar feed - either an editable standalone CalendarEvent or a
// read-only projection of another module's own dated record (FieldServiceJob/Ticket/
// ProjectTask/CustomerTask). SourceType tells the page which one it's looking at so it can
// decide whether dragging is allowed and where a click should navigate.
public class CalendarFeedItemDto
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public bool IsEditable { get; set; }
    public string Url { get; set; } = string.Empty;
}
