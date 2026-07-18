using Leitor.Erp.Entities.Governance;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Governance;

public class GetDeletionRequestListInput : PagedAndSortedResultRequestDto
{
    public DeletionRequestStatus? Status { get; set; }
}
