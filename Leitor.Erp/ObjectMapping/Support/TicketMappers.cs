using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Dtos.Support;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Support;

[Mapper]
public partial class TicketToTicketDtoMapper : MapperBase<Ticket, TicketDto>
{
    [MapperIgnoreSource(nameof(Ticket.ExtraProperties))]
    [MapperIgnoreSource(nameof(Ticket.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(TicketDto.CustomerName))]
    [MapperIgnoreTarget(nameof(TicketDto.AssignedToUserName))]
    [MapperIgnoreTarget(nameof(TicketDto.IsSlaBreached))]
    [MapperIgnoreTarget(nameof(TicketDto.ProblemNumber))]
    public override partial TicketDto Map(Ticket source);

    [MapperIgnoreSource(nameof(Ticket.ExtraProperties))]
    [MapperIgnoreSource(nameof(Ticket.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(TicketDto.CustomerName))]
    [MapperIgnoreTarget(nameof(TicketDto.AssignedToUserName))]
    [MapperIgnoreTarget(nameof(TicketDto.IsSlaBreached))]
    [MapperIgnoreTarget(nameof(TicketDto.ProblemNumber))]
    public override partial void Map(Ticket source, TicketDto destination);
}
