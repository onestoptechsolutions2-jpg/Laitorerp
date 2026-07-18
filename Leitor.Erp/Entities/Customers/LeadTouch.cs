using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// Append-only cross-channel contact log entry - CreatorId/CreationTime (from
// FullAuditedAggregateRoot) already give "who logged this and when" a timeline needs, same
// rationale as CustomerNote. TouchedAt is separate from CreationTime since a touch is often
// logged after the fact (e.g. backfilling yesterday's WhatsApp reply) - CreationTime says when it
// was entered into the system, TouchedAt says when the contact actually happened.
public class LeadTouch : FullAuditedAggregateRoot<Guid>
{
    public Guid LeadId { get; set; }
    public LeadChannel Channel { get; set; } = LeadChannel.WhatsApp;
    public LeadDirection Direction { get; set; } = LeadDirection.Outbound;
    public string? Notes { get; set; }
    public DateTime TouchedAt { get; set; }

    protected LeadTouch()
    {
    }

    public LeadTouch(Guid id, Guid leadId, LeadChannel channel, LeadDirection direction, DateTime touchedAt)
        : base(id)
    {
        LeadId = leadId;
        Channel = channel;
        Direction = direction;
        TouchedAt = touchedAt;
    }
}
