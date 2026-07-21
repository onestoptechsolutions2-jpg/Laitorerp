using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class ProductToProductDtoMapper : MapperBase<Product, ProductDto>
{
    [MapperIgnoreSource(nameof(Product.ExtraProperties))]
    [MapperIgnoreSource(nameof(Product.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProductDto.CategoryName))]
    [MapperIgnoreTarget(nameof(ProductDto.QuantityOnHand))]
    public override partial ProductDto Map(Product source);

    [MapperIgnoreSource(nameof(Product.ExtraProperties))]
    [MapperIgnoreSource(nameof(Product.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProductDto.CategoryName))]
    [MapperIgnoreTarget(nameof(ProductDto.QuantityOnHand))]
    public override partial void Map(Product source, ProductDto destination);
}
