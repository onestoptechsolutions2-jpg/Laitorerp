using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.ServiceCatalog;

// ITIL4 Service Catalog Management: a definition of a service Leitor offers, independent of any
// one customer or request. Distinct from Sales.Product (a sellable SKU) - this describes the
// service itself (who owns it, what its target response time is), and Phase 4's ServiceRequest
// optionally references one via ServiceCatalogItemId instead of a free-text description.
public class ServiceCatalogItem : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public Guid? OwnerUserId { get; set; }
    public int? TargetSlaHours { get; set; }
    public bool IsActive { get; set; } = true;

    // Which Partner actually delivers this service, if it's not delivered directly by internal
    // staff - loose reference, same convention as every other cross-aggregate link in this app
    // (see Services/Governance/DependencyGuard.cs's own comment - no real FK). Added 2026-08-17:
    // Product already has this via ProductVendor for sellable goods; services had no equivalent.
    public Guid? PartnerId { get; set; }

    protected ServiceCatalogItem()
    {
    }

    public ServiceCatalogItem(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
