using System;
using Leitor.Erp.Entities.Support;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class TicketDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? ProblemId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketType Type { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public int? CustomerSatisfactionRating { get; set; }
    public DateTime? SlaDueDate { get; set; }
    public int ReopenCount { get; set; }

    // Resolved by TicketAppService from Customer/IdentityUser/Problem repositories - not stored columns.
    public string? CustomerName { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? ProblemNumber { get; set; }

    // Computed by TicketAppService, never stored - same convention as InvoicePaymentStatus.
    public bool IsSlaBreached { get; set; }
}
