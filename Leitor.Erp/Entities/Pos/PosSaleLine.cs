using System;
using Leitor.Erp.Entities.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Pos;

// Same shape as OrderLine/InvoiceLine (snapshotted Description/UnitPrice/Cost/TaxRateId/
// TaxRatePercent at add-time) - implements ITaxableLineItem so LineMath.Subtotal()/Total() work
// unchanged.
public class PosSaleLine : FullAuditedAggregateRoot<Guid>, ITaxableLineItem
{
    public Guid PosSaleId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    protected PosSaleLine()
    {
    }

    public PosSaleLine(Guid id, Guid posSaleId, string description, decimal unitPrice)
        : base(id)
    {
        PosSaleId = posSaleId;
        Description = description;
        UnitPrice = unitPrice;
    }
}
