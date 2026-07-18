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

    // Not user-typed on the plain Create form - set by CreateFromOrder.cshtml.cs when a PO is
    // raised to fulfill a specific Sales Order.
    public Guid? SourceOrderId { get; set; }

    public bool ShipToCustomer { get; set; }
}
