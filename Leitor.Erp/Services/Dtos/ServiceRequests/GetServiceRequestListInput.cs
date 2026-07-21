using System;
using Leitor.Erp.Entities.ServiceRequests;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.ServiceRequests;

public class GetServiceRequestListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public ServiceRequestStatus? Status { get; set; }
    public string? Filter { get; set; }
}
