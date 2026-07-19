using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class OrderLine : FullAuditedAggregateRoot<Guid>
{
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }

    // Same snapshot rationale as QuoteLine.Cost/TaxRateId/TaxRatePercent.
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    protected OrderLine()
    {
    }

    public OrderLine(Guid id, Guid orderId, string description, decimal unitPrice)
        : base(id)
    {
        OrderId = orderId;
        Description = description;
        UnitPrice = unitPrice;
    }
}
