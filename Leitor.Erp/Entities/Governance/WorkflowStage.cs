namespace Leitor.Erp.Entities.Governance;

// The full Lead -> Customer -> Opportunity -> Proposal -> Quote -> Order -> Deposit Invoice ->
// Installation -> Final Invoice chain, as a flat list of milestones a WorkflowStageEvent can
// record against any entity in that chain - not a state machine on any single entity (each
// document already has its own Status enum for that), just an append-only log of "this happened."
public enum WorkflowStage
{
    LeadCreated = 0,
    LeadQualified = 1,
    CustomerCreated = 2,
    OpportunityOpened = 3,
    ProposalDraft = 4,
    ProposalSent = 5,
    ProposalApproved = 6,
    ProposalRejected = 7,
    OrderConfirmed = 8,
    DepositInvoiceIssued = 9,
    DepositPaid = 10,
    InstallationScheduled = 11,
    InstallationCompleted = 12,
    FinalInvoiceIssued = 13,
    ClosedWon = 14,
    ClosedLost = 15,
    Unlocked = 16,
    DataErased = 17
}
