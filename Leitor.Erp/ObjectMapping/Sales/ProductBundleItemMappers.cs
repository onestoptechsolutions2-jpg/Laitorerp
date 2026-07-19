using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class ProductBundleItemToProductBundleItemDtoMapper : MapperBase<ProductBundleItem, ProductBundleItemDto>
{
    [MapperIgnoreSource(nameof(ProductBundleItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProductBundleItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProductBundleItemDto.ComponentProductName))]
    public override partial ProductBundleItemDto Map(ProductBundleItem source);

    [MapperIgnoreSource(nameof(ProductBundleItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProductBundleItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProductBundleItemDto.ComponentProductName))]
    public override partial void Map(ProductBundleItem source, ProductBundleItemDto destination);
}
