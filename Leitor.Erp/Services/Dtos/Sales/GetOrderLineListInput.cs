using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetOrderLineListInput : PagedAndSortedResultRequestDto
{
    public Guid? OrderId { get; set; }
}
