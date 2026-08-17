using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Hr;

namespace Leitor.Erp.Services.Dtos.Hr;

public class CreateUpdateLeaveRequestDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    public LeaveType LeaveType { get; set; } = LeaveType.Annual;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0.5, 365)]
    public decimal DaysRequested { get; set; }

    [StringLength(2000)]
    public string? Reason { get; set; }
}
