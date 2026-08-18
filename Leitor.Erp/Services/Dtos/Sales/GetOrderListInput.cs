using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetOrderListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public string? Filter { get; set; }
    public OrderStatus? Status { get; set; }
}
