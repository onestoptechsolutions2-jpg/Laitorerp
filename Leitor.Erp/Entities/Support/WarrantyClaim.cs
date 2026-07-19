using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Support;

// A distinct lifecycle for warranty claims, separate from CustomerContract.Type == Warranty (which
// is just a commercial-agreement label) and from a Ticket (which is a general support
// conversation). All three links are optional and independent - a claim might reference the
// warranty contract, the FieldServiceJob being claimed against, and/or a Ticket the customer first
// raised it through, in any combination.
public class WarrantyClaim : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? TicketId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Open;
    public DateTime FiledDate { get; set; }

    // Auto-tracked the same way Ticket.ResolvedDate/FieldServiceJob.CompletedDate already are -
    // set the moment Status transitions into Approved/Rejected/Resolved, cleared if reopened.
    public DateTime? ResolvedDate { get; set; }

    protected WarrantyClaim()
    {
    }

    public WarrantyClaim(Guid id, Guid customerId, string claimNumber, string description)
        : base(id)
    {
        CustomerId = customerId;
        ClaimNumber = claimNumber;
        Description = description;
    }
}
