using Leitor.Erp.Entities.Partners;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Partners;

public class GetPartnerListInput : PagedAndSortedResultRequestDto
{
    public PartnerCategory? Category { get; set; }
    public string? Filter { get; set; }
}
