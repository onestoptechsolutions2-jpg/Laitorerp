using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class ProductVendorToProductVendorDtoMapper : MapperBase<ProductVendor, ProductVendorDto>
{
    [MapperIgnoreSource(nameof(ProductVendor.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProductVendor.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProductVendorDto.VendorName))]
    public override partial ProductVendorDto Map(ProductVendor source);

    [MapperIgnoreSource(nameof(ProductVendor.ExtraProperties))]
    [MapperIgnoreSource(nameof(ProductVendor.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProductVendorDto.VendorName))]
    public override partial void Map(ProductVendor source, ProductVendorDto destination);
}
