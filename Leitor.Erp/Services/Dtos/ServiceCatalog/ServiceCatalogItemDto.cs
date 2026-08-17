using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.ServiceCatalog;

public class ServiceCatalogItemDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PartnerId { get; set; }
    public int? TargetSlaHours { get; set; }
    public bool IsActive { get; set; }

    // Resolved by ServiceCatalogItemAppService - not stored columns.
    public string? OwnerUserName { get; set; }
    public string? PartnerName { get; set; }
}
