using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Support;

namespace Leitor.Erp.Services.Dtos.Workspace;

public class MyWorkspaceDto
{
    public List<MyTicketDto> Tickets { get; set; } = new();
    public List<MyJobDto> Jobs { get; set; } = new();

    // CustomerTask + (feature-gated) ProjectTask assigned to the current user, not yet completed,
    // ordered by DueDate - passive, on-screen-only reminders (see MyWorkspaceAppService), no
    // background worker/email involved.
    public List<MyReminderDto> Reminders { get; set; } = new();

    // Null when the current user doesn't hold DeletionApprovals.Decide - distinct from a genuine
    // zero, same "section only appears if you can see it" convention as DashboardAppService.
    public int? PendingDeletionRequestCount { get; set; }

    // Null when the current user doesn't hold Changes.Approve - same convention as
    // PendingDeletionRequestCount above.
    public int? PendingChangeRequestCount { get; set; }

    // Null when the current user doesn't hold Escalations.Default (can't view the Escalations
    // page at all). Unlike the two counts above, this is filtered further to only items the
    // user can actually decide (per-row RequiredPermission, or the Escalations.Decide catch-all)
    // - so it can legitimately be 0 even when pending escalations exist system-wide, if none of
    // them are this user's to act on.
    public int? PendingEscalationCount { get; set; }
}

public class MyTicketDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTime? SlaDueDate { get; set; }
}

public class MyJobDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public FieldServiceJobType Type { get; set; }
    public FieldServiceJobStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
}

public class MyReminderDto
{
    public Guid Id { get; set; }

    // "CustomerTask" or "ProjectTask" - lets the page link to the right Detail page/anchor.
    public string EntityType { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; }
}
