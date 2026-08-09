using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Opportunities;

// Not required to descend from a Lead - CustomerId is set directly so repeat business against an
// existing Customer doesn't need a fake Lead first (see LeadAppService.ConvertToCustomerAsync for
// the separate Lead -> Customer conversion). LeadId is populated when this Opportunity WAS opened
// that way (LeadAppService.ConvertToCustomerAsync auto-creates one), null for direct-entry
// Opportunities - both paths are supported side by side.
public class Opportunity : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? LeadId { get; set; }
    public string Name { get; set; } = string.Empty;
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;
    public decimal? EstimatedValue { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? LostReason { get; set; }
    public string? Notes { get; set; }

    // A Partner and/or Agent involved in delivering/referring this deal (see Entities/Partners/) -
    // both optional and independent (a deal can involve neither, either, or both at once, e.g. an
    // Agent-referred lead fulfilled through a delivery Partner). Drives the default Partner/Agent
    // choice when recording a Commission against this Opportunity.
    public Guid? PartnerId { get; set; }
    public Guid? AgentId { get; set; }

    // Auto-tracked by OpportunityAppService the moment Status transitions into Won/Lost, cleared
    // if reopened - same pattern as Ticket.ResolvedDate/FieldServiceJob.CompletedDate.
    public DateTime? ClosedDate { get; set; }

    protected Opportunity()
    {
    }

    public Opportunity(Guid id, Guid customerId, string name)
        : base(id)
    {
        CustomerId = customerId;
        Name = name;
    }
}
