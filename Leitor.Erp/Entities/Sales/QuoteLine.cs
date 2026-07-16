using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Description/UnitPrice are snapshotted at the time the line is added (ProductId is nullable to
// allow one-off line items not tied to the catalog), so later Product edits/deletes never affect
// an existing quote. LineTotal is intentionally not stored - always computed as
// UnitPrice * Quantity * (1 - DiscountPercent/100) by the app service, to avoid drift.
public class QuoteLine : FullAuditedAggregateRoot<Guid>
{
    public Guid QuoteId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }

    protected QuoteLine()
    {
    }

    public QuoteLine(Guid id, Guid quoteId, string description, decimal unitPrice)
        : base(id)
    {
        QuoteId = quoteId;
        Description = description;
        UnitPrice = unitPrice;
    }
}
