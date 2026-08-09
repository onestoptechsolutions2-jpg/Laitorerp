using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Partners;

// One calculated commission/revenue-share instance against a single Opportunity, owed to exactly
// one Partner OR one Agent (CommissionAppService enforces "exactly one", never both/neither).
// Basis/Rate/Trigger are snapshotted from the Partner/Agent's current defaults at creation time -
// same "snapshot-at-creation" convention as Quote/Order/Invoice lines snapshot UnitPrice/TaxRate -
// so a later change to the partner's standard rate never rewrites an already-recorded commission.
public class Commission : FullAuditedAggregateRoot<Guid>
{
    public Guid OpportunityId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? AgentId { get; set; }

    public CommissionBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Amount { get; set; }
    public CommissionTrigger Trigger { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;

    // Set at creation when the deal's Invoice is already known - CommissionAutoPayableService
    // watches for a Payment landing against this Invoice to flip Trigger.OnClientPayment
    // commissions from Pending to Payable. Left null if no Invoice exists yet; the commission
    // just stays Pending until someone edits it in once the Invoice is raised.
    public Guid? SourceInvoiceId { get; set; }

    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }

    protected Commission()
    {
    }

    public Commission(Guid id, Guid opportunityId)
        : base(id)
    {
        OpportunityId = opportunityId;
    }
}
