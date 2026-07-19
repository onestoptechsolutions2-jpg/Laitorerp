using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class PriceListItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }

    // Resolved by PriceListItemAppService - not a stored column.
    public string? ProductName { get; set; }
}
