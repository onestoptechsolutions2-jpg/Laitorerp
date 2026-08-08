using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

// Mirrors Entities/Customers/CustomerContact.cs exactly - a Vendor previously had only a single
// Email/Phone on the Vendor record itself; this supports multiple named contacts the way Customer
// already does.
public class VendorContact : FullAuditedAggregateRoot<Guid>
{
    public Guid VendorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }

    protected VendorContact()
    {
    }

    public VendorContact(Guid id, Guid vendorId, string fullName)
        : base(id)
    {
        VendorId = vendorId;
        FullName = fullName;
    }
}