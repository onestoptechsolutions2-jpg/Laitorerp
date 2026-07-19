using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Flat, no nesting - matches this codebase's preference for simple over deep hierarchies
// elsewhere (see CustomerContract/Ticket, both flat enums rather than tree structures).
public class ProductCategory : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;

    protected ProductCategory()
    {
    }

    public ProductCategory(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
