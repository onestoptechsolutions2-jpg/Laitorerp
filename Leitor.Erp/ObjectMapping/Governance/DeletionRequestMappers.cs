using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Services.Dtos.Governance;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Governance;

[Mapper]
public partial class DeletionRequestToDeletionRequestDtoMapper : MapperBase<DeletionRequest, DeletionRequestDto>
{
    [MapperIgnoreSource(nameof(DeletionRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(DeletionRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(DeletionRequestDto.RequestedByUserName))]
    [MapperIgnoreTarget(nameof(DeletionRequestDto.DecidedByUserName))]
    public override partial DeletionRequestDto Map(DeletionRequest source);

    [MapperIgnoreSource(nameof(DeletionRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(DeletionRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(DeletionRequestDto.RequestedByUserName))]
    [MapperIgnoreTarget(nameof(DeletionRequestDto.DecidedByUserName))]
    public override partial void Map(DeletionRequest source, DeletionRequestDto destination);
}
