using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Partners;

public class GetAgentListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
