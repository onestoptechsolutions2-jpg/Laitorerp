using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetPriceListItemListInput : PagedAndSortedResultRequestDto
{
    public Guid? PriceListId { get; set; }
    public Guid? ProductId { get; set; }
}
