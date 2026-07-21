using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Support;

namespace Leitor.Erp.Services.Dtos.Workspace;

public class MyWorkspaceDto
{
    public List<MyTicketDto> Tickets { get; set; } = new();
    public List<MyJobDto> Jobs { get; set; } = new();

    // Null when the current user doesn't hold DeletionApprovals.Decide - distinct from a genuine
    // zero, same "section only appears if you can see it" convention as DashboardAppService.
    public int? PendingDeletionRequestCount { get; set; }
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
