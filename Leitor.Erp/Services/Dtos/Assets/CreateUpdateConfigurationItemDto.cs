using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Assets;

namespace Leitor.Erp.Services.Dtos.Assets;

public class CreateUpdateConfigurationItemDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public ConfigurationItemType CIType { get; set; } = ConfigurationItemType.Hardware;

    public Guid? CustomerId { get; set; }

    [StringLength(128)]
    public string? SerialNumber { get; set; }

    public ConfigurationItemStatus Status { get; set; } = ConfigurationItemStatus.InUse;

    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public bool HasEndpointProtection { get; set; }
    public bool IsBackedUp { get; set; }
    public DateTime? LastBackupVerifiedDate { get; set; }
    public DateTime? LastPatchedDate { get; set; }
    public bool SecurityMonitoringEnabled { get; set; }
}
