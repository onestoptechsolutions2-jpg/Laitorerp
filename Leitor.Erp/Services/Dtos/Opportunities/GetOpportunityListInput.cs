using System;
using Leitor.Erp.Entities.Opportunities;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class GetOpportunityListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public OpportunityStatus? Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Filter { get; set; }
}
