using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateCustomerNoteDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public CustomerNoteType Type { get; set; } = CustomerNoteType.General;

    public LeadDirection Direction { get; set; } = LeadDirection.Outbound;

    [Required]
    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;

    public DateTime? TouchedAt { get; set; }
}
