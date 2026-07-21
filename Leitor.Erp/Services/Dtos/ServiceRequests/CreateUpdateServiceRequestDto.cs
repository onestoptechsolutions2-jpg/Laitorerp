using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.ServiceRequests;

namespace Leitor.Erp.Services.Dtos.ServiceRequests;

public class CreateUpdateServiceRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? ServiceCatalogItemId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Submitted;

    [Required]
    public DateTime RequestedDate { get; set; }
}
