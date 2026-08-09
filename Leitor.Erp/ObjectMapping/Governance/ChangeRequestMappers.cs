using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Services.Dtos.Governance;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Governance;

[Mapper]
public partial class ChangeRequestToChangeRequestDtoMapper : MapperBase<ChangeRequest, ChangeRequestDto>
{
    [MapperIgnoreSource(nameof(ChangeRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(ChangeRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ChangeRequestDto.ConfigurationItemName))]
    [MapperIgnoreTarget(nameof(ChangeRequestDto.ApprovedByUserName))]
    public override partial ChangeRequestDto Map(ChangeRequest source);

    [MapperIgnoreSource(nameof(ChangeRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(ChangeRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ChangeRequestDto.ConfigurationItemName))]
    [MapperIgnoreTarget(nameof(ChangeRequestDto.ApprovedByUserName))]
    public override partial void Map(ChangeRequest source, ChangeRequestDto destination);
}
