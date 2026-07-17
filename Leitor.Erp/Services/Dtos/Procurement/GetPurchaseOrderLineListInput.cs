using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GetPurchaseOrderLineListInput : PagedAndSortedResultRequestDto
{
    public Guid? PurchaseOrderId { get; set; }
}
