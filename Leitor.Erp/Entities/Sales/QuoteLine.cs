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

    // Snapshotted from Product.Cost at add-time (or 0 for a one-off line) - drives the internal-
    // only MarginPercent shown on Quote/Order line editing, never printed on the customer PDF.
    public decimal Cost { get; set; }

    // Snapshotted from the chosen TaxRate (or the system default) at add-time, same no-drift
    // reasoning as UnitPrice above - editing TaxRate.Percent later never changes this quote.
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

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
