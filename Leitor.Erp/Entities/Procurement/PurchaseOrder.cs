using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

public class PurchaseOrder : FullAuditedAggregateRoot<Guid>
{
    public Guid VendorId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }

    protected PurchaseOrder()
    {
    }

    public PurchaseOrder(Guid id, Guid vendorId, string poNumber)
        : base(id)
    {
        VendorId = vendorId;
        PONumber = poNumber;
    }
}
