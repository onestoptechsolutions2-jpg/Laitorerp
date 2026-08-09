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

    // "Basic security monitoring" from the managed-IT-and-cybersecurity retainer pitch - a
    // per-asset security posture snapshot, manually updated rather than pulled from a live
    // AV/EDR/backup agent (no such integration exists in this app).
    public bool HasEndpointProtection { get; set; }
    public bool IsBackedUp { get; set; }
    public DateTime? LastBackupVerifiedDate { get; set; }
    public DateTime? LastPatchedDate { get; set; }
    public bool SecurityMonitoringEnabled { get; set; }

    protected ConfigurationItem()
    {
    }

    public ConfigurationItem(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
