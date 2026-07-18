using Leitor.Erp.Entities.Customers;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class GetLeadListInput : PagedAndSortedResultRequestDto
{
    public LeadStatus? Status { get; set; }
    public string? Filter { get; set; }
}
