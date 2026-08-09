using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Services.Dtos.Partners;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Partners;

[Mapper]
public partial class PartnerToPartnerDtoMapper : MapperBase<Partner, PartnerDto>
{
    [MapperIgnoreSource(nameof(Partner.ExtraProperties))]
    [MapperIgnoreSource(nameof(Partner.ConcurrencyStamp))]
    public override partial PartnerDto Map(Partner source);

    [MapperIgnoreSource(nameof(Partner.ExtraProperties))]
    [MapperIgnoreSource(nameof(Partner.ConcurrencyStamp))]
    public override partial void Map(Partner source, PartnerDto destination);
}
