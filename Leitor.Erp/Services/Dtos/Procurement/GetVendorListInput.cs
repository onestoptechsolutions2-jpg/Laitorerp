using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GetVendorListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
