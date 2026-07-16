using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateCustomerNoteDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public CustomerNoteType Type { get; set; } = CustomerNoteType.General;

    [Required]
    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;
}
