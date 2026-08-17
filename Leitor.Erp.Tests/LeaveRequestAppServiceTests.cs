using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Hr;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 Leave Management approval workflow (Phase 4b) - reuses the generic
// EscalationItem/EscalationGate mechanism (see Services/Governance/LeaveRequestEscalationHandler.cs)
// rather than a bespoke approval table. Note: ErpTestBase's AlwaysAllowAuthorizationService means
// permission-denial branches (e.g. "a non-approver can't self-approve their own leave request")
// aren't exercisable in this harness - same documented limitation QuoteMarginGateTests/
// EscalationItemTests already carry. These tests cover the filing/approval/rejection-reconciliation
// logic itself.
public class LeaveRequestAppServiceTests : ErpTestBase
{
    private async Task<Guid> CreateEmployeeAsync(string fullName = "Test Employee")
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.HumanResources, "true", "T", null);

        var employeeAppService = GetRequiredService<EmployeeAppService>();
        var employee = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = fullName,
            HireDate = DateTime.UtcNow.AddYears(-1)
        });
        return employee.Id;
    }

    [Fact]
    public async Task SubmitAsync_Files_A_Pending_EscalationItem_With_The_Right_Shape()
    {
        var employeeId = await CreateEmployeeAsync();
        var leaveRequestAppService = GetRequiredService<LeaveRequestAppService>();

        var leaveRequest = await leaveRequestAppService.CreateAsync(new CreateUpdateLeaveRequestDto
        {
            EmployeeId = employeeId,
            LeaveType = LeaveType.Annual,
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 5),
            DaysRequested = 5,
            Reason = "Annual family trip"
        });

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => leaveRequestAppService.SubmitAsync(leaveRequest.Id));
        Assert.Equal("Leave request submitted for approval.", ex.Message);

        var reloaded = await leaveRequestAppService.GetAsync(leaveRequest.Id);
        Assert.Equal(LeaveRequestStatus.PendingApproval, reloaded.Status);

        var escalationRepository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var escalation = Assert.Single(await escalationRepository.GetListAsync(
            x => x.ActionType == LeaveRequestAppService.ApproveActionType && x.EntityId == leaveRequest.Id));

        Assert.Equal("LeaveRequest", escalation.EntityType);
        Assert.Equal(ErpPermissions.Leave.Approve, escalation.RequiredPermission);
        Assert.Equal(EscalationItemStatus.Pending, escalation.Status);
        Assert.Equal("Annual family trip", escalation.Reason);
    }

    [Fact]
    public async Task Approving_The_Escalation_Flips_LeaveRequest_Status_To_Approved()
    {
        var employeeId = await CreateEmployeeAsync();
        var leaveRequestAppService = GetRequiredService<LeaveRequestAppService>();

        var leaveRequest = await leaveRequestAppService.CreateAsync(new CreateUpdateLeaveRequestDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 11, 1),
            EndDate = new DateTime(2026, 11, 2),
            DaysRequested = 2
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => leaveRequestAppService.SubmitAsync(leaveRequest.Id));

        var escalationRepository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var escalation = Assert.Single(await escalationRepository.GetListAsync(
            x => x.ActionType == LeaveRequestAppService.ApproveActionType && x.EntityId == leaveRequest.Id));

        var escalationItemAppService = GetRequiredService<EscalationItemAppService>();
        await escalationItemAppService.ApproveAsync(escalation.Id);

        var reloaded = await leaveRequestAppService.GetAsync(leaveRequest.Id);
        Assert.Equal(LeaveRequestStatus.Approved, reloaded.Status);

        var decidedEscalation = await escalationRepository.GetAsync(escalation.Id);
        Assert.Equal(EscalationItemStatus.Approved, decidedEscalation.Status);
    }

    [Fact]
    public async Task Rejecting_The_Escalation_Is_Reflected_As_Rejected_On_Read_Without_Mutating_The_Entity_Directly()
    {
        var employeeId = await CreateEmployeeAsync();
        var leaveRequestAppService = GetRequiredService<LeaveRequestAppService>();

        var leaveRequest = await leaveRequestAppService.CreateAsync(new CreateUpdateLeaveRequestDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 3),
            DaysRequested = 3
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => leaveRequestAppService.SubmitAsync(leaveRequest.Id));

        var escalationRepository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var escalation = Assert.Single(await escalationRepository.GetListAsync(
            x => x.ActionType == LeaveRequestAppService.ApproveActionType && x.EntityId == leaveRequest.Id));

        var escalationItemAppService = GetRequiredService<EscalationItemAppService>();
        await escalationItemAppService.RejectAsync(escalation.Id, "Coverage gap during that period");

        // The underlying LeaveRequest row itself still says PendingApproval - RejectAsync never
        // touches domain entities, only the EscalationItem. The Rejected status is reconciled at
        // read time in LeaveRequestAppService.GetAsync/GetListAsync instead.
        var reloaded = await leaveRequestAppService.GetAsync(leaveRequest.Id);
        Assert.Equal(LeaveRequestStatus.Rejected, reloaded.Status);
    }

    [Fact]
    public async Task SubmitAsync_Twice_Is_Blocked_By_The_Duplicate_Pending_Escalation_Guard()
    {
        var employeeId = await CreateEmployeeAsync();
        var leaveRequestAppService = GetRequiredService<LeaveRequestAppService>();

        var leaveRequest = await leaveRequestAppService.CreateAsync(new CreateUpdateLeaveRequestDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 10, 10),
            EndDate = new DateTime(2026, 10, 11),
            DaysRequested = 1
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => leaveRequestAppService.SubmitAsync(leaveRequest.Id));

        // Status is already PendingApproval, not Draft - SubmitAsync's own guard blocks this
        // before EscalationGate.FileAsync's dedup check would even run.
        var secondAttempt = await Assert.ThrowsAsync<UserFriendlyException>(() => leaveRequestAppService.SubmitAsync(leaveRequest.Id));
        Assert.Equal("Only a Draft leave request can be submitted for approval.", secondAttempt.Message);
    }
}
