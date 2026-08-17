using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class PriceListItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PriceListId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public RateType RateType { get; set; }

    // Resolved by PriceListItemAppService - not stored columns.
    public string? ProductName { get; set; }
    public string? ServiceCatalogItemName { get; set; }
}
