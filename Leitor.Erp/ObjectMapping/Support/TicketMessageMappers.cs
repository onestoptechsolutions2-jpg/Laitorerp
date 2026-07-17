using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Dtos.Support;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Support;

[Mapper]
public partial class TicketMessageToTicketMessageDtoMapper : MapperBase<TicketMessage, TicketMessageDto>
{
    [MapperIgnoreSource(nameof(TicketMessage.ExtraProperties))]
    [MapperIgnoreSource(nameof(TicketMessage.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(TicketMessageDto.CreatorUserName))]
    public override partial TicketMessageDto Map(TicketMessage source);

    [MapperIgnoreSource(nameof(TicketMessage.ExtraProperties))]
    [MapperIgnoreSource(nameof(TicketMessage.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(TicketMessageDto.CreatorUserName))]
    public override partial void Map(TicketMessage source, TicketMessageDto destination);
}
