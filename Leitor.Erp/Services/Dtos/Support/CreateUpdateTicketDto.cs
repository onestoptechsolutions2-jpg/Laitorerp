using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Support;

namespace Leitor.Erp.Services.Dtos.Support;

public class CreateUpdateTicketDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? JobId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? ProblemId { get; set; }

    [Required]
    [StringLength(256)]
    public string Subject { get; set; } = string.Empty;

    public TicketType Type { get; set; } = TicketType.General;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public Guid? AssignedToUserId { get; set; }

    [Range(1, 5)]
    public int? CustomerSatisfactionRating { get; set; }
}
