using System;
using Leitor.Erp.Entities.ServiceRequests;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.ServiceRequests;

public class ServiceRequestDto : FullAuditedEntityDto<Guid>
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public ServiceRequestStatus Status { get; set; }
    public DateTime RequestedDate { get; set; }
    public DateTime? FulfilledDate { get; set; }

    // Resolved by ServiceRequestAppService - not stored columns.
    public string? CustomerName { get; set; }
    public string? ServiceCatalogItemName { get; set; }
}
