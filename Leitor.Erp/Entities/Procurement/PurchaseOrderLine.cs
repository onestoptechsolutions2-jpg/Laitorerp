using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

public class PurchaseOrderLine : FullAuditedAggregateRoot<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }

    protected PurchaseOrderLine()
    {
    }

    public PurchaseOrderLine(Guid id, Guid purchaseOrderId, string description, decimal unitPrice)
        : base(id)
    {
        PurchaseOrderId = purchaseOrderId;
        Description = description;
        UnitPrice = unitPrice;
    }
}
