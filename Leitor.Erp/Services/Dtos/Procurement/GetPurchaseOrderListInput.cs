using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GetPurchaseOrderListInput : PagedAndSortedResultRequestDto
{
    public Guid? VendorId { get; set; }
    public string? Filter { get; set; }
}
