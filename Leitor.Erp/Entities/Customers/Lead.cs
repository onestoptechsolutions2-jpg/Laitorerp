using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// Lightweight top-of-funnel capture, deliberately separate from Customer: a raw enquiry (name,
// contact, source) rarely comes with the address/account-owner detail a real Customer record
// carries, and mixing unqualified enquiries into the Customers list makes that list useless as a
// client roster. ConvertedCustomerId is set when a Lead graduates via
// LeadAppService.ConvertToCustomerAsync.
public class Lead : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedCustomerId { get; set; }

    // Consent/compliance: the system refuses to log further outreach (see
    // LeadTouchAppService.CreateAsync) against a lead flagged Do Not Contact - it can't stop
    // someone messaging on personal WhatsApp, but it won't record or encourage it.
    public bool DoNotContact { get; set; }

    // Digits-only normalization of Phone, computed by LeadAppService.CopyToEntity - the standing
    // dedup key, replacing the one-off phone-normalization script this used to require.
    public string? NormalizedPhone { get; set; }

    protected Lead()
    {
    }

    public Lead(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
