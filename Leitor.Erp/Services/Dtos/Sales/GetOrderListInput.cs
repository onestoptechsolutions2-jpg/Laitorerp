using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetOrderListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public string? Filter { get; set; }
}
