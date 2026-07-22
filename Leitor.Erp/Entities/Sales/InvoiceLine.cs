using System;
using Leitor.Erp.Entities.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class InvoiceLine : FullAuditedAggregateRoot<Guid>, ITaxableLineItem
{
    public Guid InvoiceId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }

    // Same snapshot rationale as QuoteLine.TaxRateId/TaxRatePercent - no Cost/margin here, since
    // an invoice mirrors its source Order 1:1 rather than being independently margin-tracked.
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    protected InvoiceLine()
    {
    }

    public InvoiceLine(Guid id, Guid invoiceId, string description, decimal unitPrice)
        : base(id)
    {
        InvoiceId = invoiceId;
        Description = description;
        UnitPrice = unitPrice;
    }
}
