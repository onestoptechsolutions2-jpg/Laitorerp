using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Leitor.Erp.Services.Partners;

// Called from PaymentAppService the moment a Payment posts against an Invoice - the concrete
// mechanism behind "commission becomes payable when the client pays" (TC-025). Only ever touches
// Commissions that are still Pending and whose Trigger is OnClientPayment; OnProposalAccepted
// commissions are already Payable from the moment CommissionAppService creates them. Same
// static-method-with-injected-deps shape as DeletionGate/WorkflowStageLog, so PaymentAppService
// (core Sales, always-on) doesn't need a hard DI dependency on the togglable Partner/Commission
// module beyond the repository itself.
public static class CommissionAutoPayableService
{
    public static async Task MarkPayableForInvoiceAsync(
        IRepository<Commission, Guid> commissionRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IGuidGenerator guidGenerator,
        ICurrentUser currentUser,
        IClock clock,
        Guid invoiceId)
    {
        var commissions = await commissionRepository.GetListAsync(x =>
            x.SourceInvoiceId == invoiceId &&
            x.Trigger == CommissionTrigger.OnClientPayment &&
            x.Status == CommissionStatus.Pending);

        foreach (var commission in commissions)
        {
            commission.Status = CommissionStatus.Payable;
            await commissionRepository.UpdateAsync(commission, autoSave: true);

            await WorkflowStageLog.RecordAsync(
                stageEventRepository, guidGenerator, currentUser, clock,
                "Commission", commission.Id, WorkflowStage.CommissionAccrued,
                notes: "Marked payable - client payment received");
        }
    }
}
