using System;
using Leitor.Erp.Entities.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

public class PurchaseOrderLine : FullAuditedAggregateRoot<Guid>, ITaxableLineItem
{
    public Guid PurchaseOrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }

    // Snapshotted at add-time via TaxRateResolver, same discipline as Entities/Sales/OrderLine.cs -
    // closes the "no tax modeling on Procurement lines" gap that previously forced
    // VatReturnAppService to approximate Input VAT off a single default rate applied to line
    // totals instead of each line's real rate.
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

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
