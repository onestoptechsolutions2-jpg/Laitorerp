using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class GetNeedsAssessmentListInput : PagedAndSortedResultRequestDto
{
    public Guid? OpportunityId { get; set; }
}
