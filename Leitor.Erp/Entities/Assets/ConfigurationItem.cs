using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Assets;

// ITIL4/CMDB: a tracked asset - typically equipment Leitor has installed or maintains at a
// customer site (the one place this genuinely fits Leitor's actual business, per the ITSM audit's
// own reservation about this module being a stretch for a customer-facing ERP). CustomerId is
// optional since some assets (spares, internal equipment) belong to no customer site yet.
public class ConfigurationItem : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public ConfigurationItemType CIType { get; set; } = ConfigurationItemType.Hardware;
    public Guid? CustomerId { get; set; }
    public string? SerialNumber { get; set; }
    public ConfigurationItemStatus Status { get; set; } = ConfigurationItemStatus.InUse;
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? Notes { get; set; }

    protected ConfigurationItem()
    {
    }

    public ConfigurationItem(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
