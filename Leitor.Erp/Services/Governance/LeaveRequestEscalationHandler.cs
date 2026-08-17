using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Services.Hr;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Governance;

// Registered explicitly in ErpModule.ConfigureServices, same as QuoteMarginOverrideEscalationHandler/
// OrderMarginOverrideEscalationHandler - see that registration's own comment on why the
// ITransientDependency convention doesn't pick these up. No payload needed (unlike the margin-
// override handlers): approving a leave request needs no extra parameters beyond "yes", so
// EscalationItem.PayloadJson stays null for this ActionType.
public class LeaveRequestEscalationHandler : IEscalationActionHandler
{
    private readonly IRepository<LeaveRequest, Guid> _leaveRequestRepository;

    public LeaveRequestEscalationHandler(IRepository<LeaveRequest, Guid> leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public string ActionType => LeaveRequestAppService.ApproveActionType;

    public async Task ExecuteAsync(EscalationItem item)
    {
        var leaveRequest = await _leaveRequestRepository.GetAsync(item.EntityId);
        leaveRequest.Status = LeaveRequestStatus.Approved;
        await _leaveRequestRepository.UpdateAsync(leaveRequest);
    }
}
