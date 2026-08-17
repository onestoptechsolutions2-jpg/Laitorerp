using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class GetEmployeeListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
