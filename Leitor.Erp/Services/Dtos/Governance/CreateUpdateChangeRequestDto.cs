using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Governance;

namespace Leitor.Erp.Services.Dtos.Governance;

public class CreateUpdateChangeRequestDto
{
    [Required]
    public Guid ConfigurationItemId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ChangeTier Tier { get; set; } = ChangeTier.Normal;

    public Guid? TicketId { get; set; }

    public DateTime? PlannedDate { get; set; }
}
