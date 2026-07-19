using System;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Governance;

// One row per Opportunity, aggregating its traceable chain (Proposal -> Quote -> Order ->
// Milestones/Invoices -> FieldServiceJobs) for the Workflow Monitor page. A Quote/Order created
// without going through ProposalAppService.ConvertToQuoteAsync/QuoteAppService.ConvertToOrderAsync
// has no link back to an Opportunity in the current data model, so it can't appear here - same
// traceability limits the rest of the app already has.
public class WorkflowMonitorRowDto
{
    public Guid OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public OpportunityStatus OpportunityStatus { get; set; }

    public Guid? ProposalId { get; set; }
    public string? ProposalNumber { get; set; }
    public ProposalStatus? ProposalStatus { get; set; }

    public Guid? QuoteId { get; set; }
    public string? QuoteNumber { get; set; }
    public QuoteStatus? QuoteStatus { get; set; }

    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public OrderStatus? OrderStatus { get; set; }

    public bool HasDepositInvoice { get; set; }
    public bool HasInstallationJob { get; set; }
    public bool InstallationCompleted { get; set; }
    public bool HasFinalInvoice { get; set; }

    public string CurrentStage { get; set; } = string.Empty;
    public string? PendingAction { get; set; }
}
