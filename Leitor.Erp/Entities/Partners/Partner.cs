using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Partners;

// A company that delivers on Laitor's behalf (POS provider, software vendor, freelance/contract
// outfit) - the delivery-side counterpart to Vendor (which models the supply side: who sells
// Laitor things). Modeled as its own aggregate root rather than folded into Vendor because the
// two roles rarely overlap and Vendor's shape (payment terms, price lists) doesn't fit a delivery
// partner. CommissionBasis/Rate/Trigger are this partner's default deal terms - snapshotted onto
// each Commission at creation (see Commission.cs) so a later rate change never rewrites history.
public class Partner : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public PartnerCategory Category { get; set; } = PartnerCategory.Other;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public CommissionBasis CommissionBasis { get; set; } = CommissionBasis.Percentage;
    public decimal CommissionRate { get; set; }
    public CommissionTrigger CommissionTrigger { get; set; } = CommissionTrigger.OnClientPayment;

    protected Partner()
    {
    }

    public Partner(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
