using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateInvoiceDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? OrderId { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
