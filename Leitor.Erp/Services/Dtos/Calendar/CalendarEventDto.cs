using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Calendar;

public class CalendarEventDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? AgentId { get; set; }

    // Resolved by CalendarEventAppService - not stored columns.
    public string? AssignedToUserName { get; set; }
    public string? AgentName { get; set; }
}
