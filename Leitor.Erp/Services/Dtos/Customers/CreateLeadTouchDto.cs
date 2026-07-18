using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateLeadTouchDto
{
    [Required]
    public Guid LeadId { get; set; }

    public LeadChannel Channel { get; set; } = LeadChannel.WhatsApp;

    public LeadDirection Direction { get; set; } = LeadDirection.Outbound;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTime? TouchedAt { get; set; }
}
