using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class PriceListItem : FullAuditedAggregateRoot<Guid>
{
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }

    protected PriceListItem()
    {
    }

    public PriceListItem(Guid id, Guid priceListId, Guid productId, decimal unitPrice)
        : base(id)
    {
        PriceListId = priceListId;
        ProductId = productId;
        UnitPrice = unitPrice;
    }
}
