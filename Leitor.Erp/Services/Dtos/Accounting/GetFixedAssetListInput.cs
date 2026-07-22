using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class GetFixedAssetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
