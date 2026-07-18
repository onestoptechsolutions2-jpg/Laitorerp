using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetProductVendorListInput : PagedAndSortedResultRequestDto
{
    public Guid? ProductId { get; set; }
}
