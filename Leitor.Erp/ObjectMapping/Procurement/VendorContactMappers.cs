using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class VendorContactToVendorContactDtoMapper : MapperBase<VendorContact, VendorContactDto>
{
    [MapperIgnoreSource(nameof(VendorContact.ExtraProperties))]
    [MapperIgnoreSource(nameof(VendorContact.ConcurrencyStamp))]
    public override partial VendorContactDto Map(VendorContact source);

    [MapperIgnoreSource(nameof(VendorContact.ExtraProperties))]
    [MapperIgnoreSource(nameof(VendorContact.ConcurrencyStamp))]
    public override partial void Map(VendorContact source, VendorContactDto destination);
}