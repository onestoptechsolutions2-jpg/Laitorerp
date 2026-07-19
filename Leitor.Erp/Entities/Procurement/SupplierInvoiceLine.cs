using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

// Mirrors PurchaseOrderLine's fields exactly (no tax modeling) since these lines are typically
// seeded from the PO's own lines - see SupplierInvoices/Create.cshtml.cs.
public class SupplierInvoiceLine : FullAuditedAggregateRoot<Guid>
{
    public Guid SupplierInvoiceId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }

    protected SupplierInvoiceLine()
    {
    }

    public SupplierInvoiceLine(Guid id, Guid supplierInvoiceId, string description, decimal unitPrice)
        : base(id)
    {
        SupplierInvoiceId = supplierInvoiceId;
        Description = description;
        UnitPrice = unitPrice;
    }
}
