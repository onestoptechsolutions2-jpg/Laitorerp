using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Procurement;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class CreateUpdateSupplierInvoiceDto
{
    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public Guid VendorId { get; set; }

    [Required]
    [StringLength(64)]
    public string SupplierInvoiceNumber { get; set; } = string.Empty;

    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
