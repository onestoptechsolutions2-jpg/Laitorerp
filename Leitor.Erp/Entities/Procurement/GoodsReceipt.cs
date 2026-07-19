using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

// One receiving event against a PurchaseOrder - a PO can be received in multiple partial
// shipments, each recorded as its own GoodsReceipt with one GoodsReceiptLine per PurchaseOrderLine
// actually received in that shipment. This is the "goods received" leg of the three-way match
// (PO / Receipt / Supplier Invoice).
public class GoodsReceipt : FullAuditedAggregateRoot<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? Notes { get; set; }

    protected GoodsReceipt()
    {
    }

    public GoodsReceipt(Guid id, Guid purchaseOrderId, DateTime receivedDate)
        : base(id)
    {
        PurchaseOrderId = purchaseOrderId;
        ReceivedDate = receivedDate;
    }
}
