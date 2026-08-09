using System;
using Leitor.Erp.Entities.Partners;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Partners;

public class GetCommissionListInput : PagedAndSortedResultRequestDto
{
    public Guid? OpportunityId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? AgentId { get; set; }
    public CommissionStatus? Status { get; set; }
}
