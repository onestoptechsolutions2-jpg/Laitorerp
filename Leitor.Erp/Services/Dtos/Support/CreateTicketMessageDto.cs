using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Support;

public class CreateTicketMessageDto
{
    [Required]
    public Guid TicketId { get; set; }

    public bool IsCustomerMessage { get; set; }

    [Required]
    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;
}
