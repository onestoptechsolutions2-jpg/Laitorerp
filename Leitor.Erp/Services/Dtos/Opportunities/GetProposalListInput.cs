using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class GetProposalListInput : PagedAndSortedResultRequestDto
{
    public Guid? OpportunityId { get; set; }
}
