using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Procurement;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class CreateUpdatePurchaseOrderDto
{
    [Required]
    public Guid VendorId { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
