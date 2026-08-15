using Leitor.Erp.Entities.Governance;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Governance;

public class GetEscalationItemListInput : PagedAndSortedResultRequestDto
{
    public EscalationItemStatus? Status { get; set; }
    public string? ActionType { get; set; }
}
