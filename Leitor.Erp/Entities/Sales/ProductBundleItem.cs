using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class ProductBundleItem : FullAuditedAggregateRoot<Guid>
{
    public Guid BundleProductId { get; set; }
    public Guid ComponentProductId { get; set; }
    public decimal Quantity { get; set; } = 1;

    protected ProductBundleItem()
    {
    }

    public ProductBundleItem(Guid id, Guid bundleProductId, Guid componentProductId)
        : base(id)
    {
        BundleProductId = bundleProductId;
        ComponentProductId = componentProductId;
    }
}
