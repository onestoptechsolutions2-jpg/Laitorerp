using System;
using Leitor.Erp.Entities.Hr;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class LeaveRequestDto : FullAuditedEntityDto<Guid>
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; }

    // Resolved by LeaveRequestAppService - not a stored column.
    public string? EmployeeName { get; set; }
}
