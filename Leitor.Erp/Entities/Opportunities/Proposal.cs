using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Opportunities;

// The technical narrative document ahead of a commercial Quote. Deliberately has no line items /
// Bill of Materials of its own - a Proposal's BOM is whatever Quote it generates via
// ProposalAppService.ConvertToQuoteAsync (see Quote.ProposalId), so the same product list is never
// maintained in two places.
public class Proposal : FullAuditedAggregateRoot<Guid>
{
    public Guid OpportunityId { get; set; }
    public string ProposalNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    public int Version { get; set; } = 1;

    public string? Summary { get; set; }
    public string? ProposedSolution { get; set; }
    public string? Scope { get; set; }
    public string? Timeline { get; set; }
    public string? Assumptions { get; set; }
    public string? Exclusions { get; set; }
    public string? WarrantyAndSupport { get; set; }
    public string? Terms { get; set; }

    protected Proposal()
    {
    }

    public Proposal(Guid id, Guid opportunityId, string proposalNumber, string title)
        : base(id)
    {
        OpportunityId = opportunityId;
        ProposalNumber = proposalNumber;
        Title = title;
    }
}
