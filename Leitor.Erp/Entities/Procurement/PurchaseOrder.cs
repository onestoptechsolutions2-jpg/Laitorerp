using System;
using Leitor.Erp.Entities.Sales;
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

    // Dropship traceability: which Sales Order this PO is fulfilling, and whether the vendor
    // ships straight to that order's customer (dropship) or to Leitor itself (normal
    // procurement) - a per-PO choice, since the same business does both.
    public Guid? SourceOrderId { get; set; }
    public bool ShipToCustomer { get; set; }

    // Snapshotted at creation/edit time via CurrencyRateResolver, never recomputed later - same
    // discipline as Entities/Sales/Quote.cs.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    // Defaulted from Vendor.DefaultPaymentTerms at creation, editable afterwards - mirrors
    // Order.PaymentTerms. Carried onto the SupplierInvoice created from this PO's own default,
    // not copied directly (a PO's terms can be renegotiated per invoice).
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    // Which stock location expects this PO's goods - defaults to the warehouse flagged
    // IsDefault, editable for multi-location shops. Suggested (not enforced) as the receiving
    // warehouse when creating a GoodsReceipt against this PO - see GoodsReceiptAppService.CreateAsync.
    public Guid WarehouseId { get; set; }

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
