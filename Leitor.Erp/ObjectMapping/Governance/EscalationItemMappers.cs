using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Services.Dtos.Governance;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Governance;

[Mapper]
public partial class EscalationItemToEscalationItemDtoMapper : MapperBase<EscalationItem, EscalationItemDto>
{
    [MapperIgnoreSource(nameof(EscalationItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(EscalationItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(EscalationItemDto.RequestedByUserName))]
    [MapperIgnoreTarget(nameof(EscalationItemDto.DecidedByUserName))]
    public override partial EscalationItemDto Map(EscalationItem source);

    [MapperIgnoreSource(nameof(EscalationItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(EscalationItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(EscalationItemDto.RequestedByUserName))]
    [MapperIgnoreTarget(nameof(EscalationItemDto.DecidedByUserName))]
    public override partial void Map(EscalationItem source, EscalationItemDto destination);
}
