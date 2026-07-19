using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class ProductCategoryToProductCategoryDtoMapper : MapperBase<ProductCategory, ProductCategoryDto>
{
    [MapperIgnoreSource(nameof(ProductCategory.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProductCategory.ConcurrencyStamp))]
    public override partial ProductCategoryDto Map(ProductCategory source);

    [MapperIgnoreSource(nameof(ProductCategory.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProductCategory.ConcurrencyStamp))]
    public override partial void Map(ProductCategory source, ProductCategoryDto destination);
}
