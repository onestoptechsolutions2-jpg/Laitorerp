using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class GetCustomerListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
