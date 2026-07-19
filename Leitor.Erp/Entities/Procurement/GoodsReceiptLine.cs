using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

public class GoodsReceiptLine : FullAuditedAggregateRoot<Guid>
{
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }

    protected GoodsReceiptLine()
    {
    }

    public GoodsReceiptLine(Guid id, Guid goodsReceiptId, Guid purchaseOrderLineId, decimal quantityReceived)
        : base(id)
    {
        GoodsReceiptId = goodsReceiptId;
        PurchaseOrderLineId = purchaseOrderLineId;
        QuantityReceived = quantityReceived;
    }
}
