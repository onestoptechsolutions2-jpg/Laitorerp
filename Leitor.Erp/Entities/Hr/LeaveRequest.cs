using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Hr;

// Approval itself is NOT modeled here - see Services/Governance/LeaveRequestEscalationHandler.cs.
// SubmitAsync (Services/Hr/LeaveRequestAppService.cs) flips Status to PendingApproval and files a
// generic EscalationItem via EscalationGate.FileAsync, same mechanism as the Quote/Order margin
// gate - reuses the existing approve/reject/audit-trail machinery instead of building a parallel
// one for Leave specifically.
public class LeaveRequest : FullAuditedAggregateRoot<Guid>
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; } = LeaveType.Annual;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Half-days possible - manually entered, not auto-computed from Start/End (that would need to
    // account for weekends/public holidays, out of scope here).
    public decimal DaysRequested { get; set; }

    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;

    protected LeaveRequest()
    {
    }

    public LeaveRequest(Guid id, Guid employeeId, LeaveType leaveType, DateTime startDate, DateTime endDate, decimal daysRequested)
        : base(id)
    {
        EmployeeId = employeeId;
        LeaveType = leaveType;
        StartDate = startDate;
        EndDate = endDate;
        DaysRequested = daysRequested;
    }
}
