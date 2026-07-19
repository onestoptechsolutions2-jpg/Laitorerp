using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetOrderPaymentMilestoneListInput : PagedAndSortedResultRequestDto
{
    public Guid? OrderId { get; set; }
}
