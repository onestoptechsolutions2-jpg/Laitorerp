using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class PriceListItemToPriceListItemDtoMapper : MapperBase<PriceListItem, PriceListItemDto>
{
    [MapperIgnoreSource(nameof(PriceListItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(PriceListItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PriceListItemDto.ProductName))]
    public override partial PriceListItemDto Map(PriceListItem source);

    [MapperIgnoreSource(nameof(PriceListItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(PriceListItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PriceListItemDto.ProductName))]
    public override partial void Map(PriceListItem source, PriceListItemDto destination);
}
