using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Calendar;

public class CreateUpdateCalendarEventDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? AgentId { get; set; }
}
