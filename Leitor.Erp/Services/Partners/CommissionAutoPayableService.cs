using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Leitor.Erp.Services.Partners;

// Called from PaymentAppService the moment a Payment posts against an Invoice - the concrete
// mechanism behind "commission becomes payable when the client pays" (TC-025). Only ever touches
// Commissions that are still Pending and whose Trigger is OnClientPayment; OnProposalAccepted
// commissions are already Payable from the moment they're created (see CommissionAppService,
// CommissionAutoCreateService). Same static-method-with-injected-deps shape as
// DeletionGate/WorkflowStageLog, so PaymentAppService (core Sales, always-on) doesn't need a hard
// DI dependency on the togglable Partner/Commission module beyond the repositories themselves.
public static class CommissionAutoPayableService
{
    public static async Task MarkPayableForInvoiceAsync(
        IRepository<Commission, Guid> commissionRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IGuidGenerator guidGenerator,
        ICurrentUser currentUser,
        IClock clock,
        Guid invoiceId)
    {
        var commissions = (await commissionRepository.GetListAsync(x =>
            x.SourceInvoiceId == invoiceId &&
            x.Trigger == CommissionTrigger.OnClientPayment &&
            x.Status == CommissionStatus.Pending)).ToList();

        // CommissionAutoCreateService auto-creates an OnClientPayment commission at proposal
        // acceptance, long before any Invoice exists, so it has no SourceInvoiceId to match on here
        // yet - resolve it the other way, by tracing this Invoice back to the Opportunity it
        // originated from (Invoice -> Order -> Quote -> Proposal -> Opportunity, the same chain
        // ProposalAppService.ConvertToQuoteAsync/OrderAppService build going forward). Any nullable
        // link missing along the way (a standalone Invoice with no Order, an Order with no Quote,
        // etc.) just means there's nothing to resolve - same "loose reference" convention used
        // everywhere else in this app.
        var opportunityId = await ResolveOpportunityIdAsync(invoiceRepository, orderRepository, quoteRepository, proposalRepository, invoiceId);
        if (opportunityId.HasValue)
        {
            var unlinked = await commissionRepository.GetListAsync(x =>
                x.OpportunityId == opportunityId.Value &&
                x.Trigger == CommissionTrigger.OnClientPayment &&
                x.Status == CommissionStatus.Pending &&
                x.SourceInvoiceId == null);

            foreach (var commission in unlinked)
            {
                commission.SourceInvoiceId = invoiceId;
                commissions.Add(commission);
            }
        }

        foreach (var commission in commissions.DistinctBy(x => x.Id))
        {
            commission.Status = CommissionStatus.Payable;
            await commissionRepository.UpdateAsync(commission, autoSave: true);

            await WorkflowStageLog.RecordAsync(
                stageEventRepository, guidGenerator, currentUser, clock,
                "Commission", commission.Id, WorkflowStage.CommissionAccrued,
                notes: "Marked payable - client payment received");
        }
    }

    private static async Task<Guid?> ResolveOpportunityIdAsync(
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Proposal, Guid> proposalRepository,
        Guid invoiceId)
    {
        var invoice = await invoiceRepository.FindAsync(invoiceId);
        if (invoice?.OrderId == null)
        {
            return null;
        }

        var order = await orderRepository.FindAsync(invoice.OrderId.Value);
        if (order?.QuoteId == null)
        {
            return null;
        }

        var quote = await quoteRepository.FindAsync(order.QuoteId.Value);
        if (quote?.ProposalId == null)
        {
            return null;
        }

        var proposal = await proposalRepository.FindAsync(quote.ProposalId.Value);
        return proposal?.OpportunityId;
    }
}
