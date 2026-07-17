using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class VendorToVendorDtoMapper : MapperBase<Vendor, VendorDto>
{
    [MapperIgnoreSource(nameof(Vendor.ExtraProperties))]
    [MapperIgnoreSource(nameof(Vendor.ConcurrencyStamp))]
    public override partial VendorDto Map(Vendor source);

    [MapperIgnoreSource(nameof(Vendor.ExtraProperties))]
    [MapperIgnoreSource(nameof(Vendor.ConcurrencyStamp))]
    public override partial void Map(Vendor source, VendorDto destination);
}
