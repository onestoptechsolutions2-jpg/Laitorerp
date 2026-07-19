using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Dtos.Support;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Support;

[Mapper]
public partial class WarrantyClaimToWarrantyClaimDtoMapper : MapperBase<WarrantyClaim, WarrantyClaimDto>
{
    [MapperIgnoreSource(nameof(WarrantyClaim.ExtraProperties))]
    [MapperIgnoreSource(nameof(WarrantyClaim.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(WarrantyClaimDto.CustomerName))]
    public override partial WarrantyClaimDto Map(WarrantyClaim source);

    [MapperIgnoreSource(nameof(WarrantyClaim.ExtraProperties))]
    [MapperIgnoreSource(nameof(WarrantyClaim.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(WarrantyClaimDto.CustomerName))]
    public override partial void Map(WarrantyClaim source, WarrantyClaimDto destination);
}
