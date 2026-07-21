using System;
using Leitor.Erp.Entities.Assets;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Assets;

public class ConfigurationItemDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public ConfigurationItemType CIType { get; set; }
    public Guid? CustomerId { get; set; }
    public string? SerialNumber { get; set; }
    public ConfigurationItemStatus Status { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? Notes { get; set; }

    // Resolved by ConfigurationItemAppService - not a stored column.
    public string? CustomerName { get; set; }
}
