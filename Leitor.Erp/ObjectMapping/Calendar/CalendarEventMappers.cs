using Leitor.Erp.Entities.Calendar;
using Leitor.Erp.Services.Dtos.Calendar;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Calendar;

[Mapper]
public partial class CalendarEventToCalendarEventDtoMapper : MapperBase<CalendarEvent, CalendarEventDto>
{
    [MapperIgnoreSource(nameof(CalendarEvent.ExtraProperties))]
    [MapperIgnoreSource(nameof(CalendarEvent.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CalendarEventDto.AssignedToUserName))]
    [MapperIgnoreTarget(nameof(CalendarEventDto.AgentName))]
    public override partial CalendarEventDto Map(CalendarEvent source);

    [MapperIgnoreSource(nameof(CalendarEvent.ExtraProperties))]
    [MapperIgnoreSource(nameof(CalendarEvent.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CalendarEventDto.AssignedToUserName))]
    [MapperIgnoreTarget(nameof(CalendarEventDto.AgentName))]
    public override partial void Map(CalendarEvent source, CalendarEventDto destination);
}
