using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetProductBundleItemListInput : PagedAndSortedResultRequestDto
{
    public Guid? BundleProductId { get; set; }
}
