using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.ServiceRequests;

// ITIL4 Service Request Management: deliberately a separate entity from Ticket, matching ITIL4's
// own distinction between Incident and Request management rather than overloading TicketType. A
// request is "please do this standard, pre-approved thing" (optionally against a
// ServiceCatalogItem), not "something is broken."
public class ServiceRequest : FullAuditedAggregateRoot<Guid>
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Submitted;
    public DateTime RequestedDate { get; set; }

    // Auto-tracked the same way Ticket.ResolvedDate/WarrantyClaim.ResolvedDate already are - set
    // the moment Status transitions into Fulfilled/Rejected, cleared if reopened.
    public DateTime? FulfilledDate { get; set; }

    protected ServiceRequest()
    {
    }

    public ServiceRequest(Guid id, string requestNumber, Guid customerId, string description)
        : base(id)
    {
        RequestNumber = requestNumber;
        CustomerId = customerId;
        Description = description;
    }
}
