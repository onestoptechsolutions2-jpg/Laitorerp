using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

// The "invoice received" leg of the three-way match - a vendor's own bill against a PurchaseOrder.
// Distinct from Entities/Sales/Invoice.cs, which is what Leitor issues to its customers; this is
// what a vendor issues to Leitor.
public class SupplierInvoice : FullAuditedAggregateRoot<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public Guid VendorId { get; set; }

    // The vendor's own invoice reference, not one of ours - free text, not DocumentNumbering.
    public string SupplierInvoiceNumber { get; set; } = string.Empty;

    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }

    // Snapshotted at creation/edit time via CurrencyRateResolver, never recomputed later.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    protected SupplierInvoice()
    {
    }

    public SupplierInvoice(Guid id, Guid purchaseOrderId, Guid vendorId, string supplierInvoiceNumber)
        : base(id)
    {
        PurchaseOrderId = purchaseOrderId;
        VendorId = vendorId;
        SupplierInvoiceNumber = supplierInvoiceNumber;
    }
}
