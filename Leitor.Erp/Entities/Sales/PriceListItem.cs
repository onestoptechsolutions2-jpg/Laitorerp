using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Exactly one of ProductId/ServiceCatalogItemId is set per row (enforced in
// PriceListItemAppService, not here - entities in this app don't self-validate cross-field
// invariants, matching every other entity's convention). ProductId stayed non-nullable Guid
// historically; ServiceCatalogItemId is the new nullable half of this either/or pair - existing
// rows are unaffected (ProductId already always had a real value for them).
public class PriceListItem : FullAuditedAggregateRoot<Guid>
{
    public Guid PriceListId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public decimal UnitPrice { get; set; }

    // Only meaningful when ServiceCatalogItemId is set - see RateType's own comment.
    public RateType RateType { get; set; } = RateType.Fixed;

    protected PriceListItem()
    {
    }

    public PriceListItem(Guid id, Guid priceListId, decimal unitPrice)
        : base(id)
    {
        PriceListId = priceListId;
        UnitPrice = unitPrice;
    }
}
