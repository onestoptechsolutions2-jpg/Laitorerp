using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Partners;

// A person who refers business or does field work for Laitor without being an employee (e.g.
// "Riffat" referring Mnyanga). Territory/Skills are free text, not a dedicated Territory/Skill
// entity - Laitor operates in a handful of named areas today, not a formal geography hierarchy;
// promote to a real entity only once routing-by-territory logic actually needs to query it
// structurally. Lead.ReferrerAgentId points here, which is how a referral relationship is recorded
// and preserved (see TC-007/TC-008 in the acceptance test suite).
public class Agent : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Territory { get; set; }
    public string? Skills { get; set; }
    public string? Notes { get; set; }

    public CommissionBasis CommissionBasis { get; set; } = CommissionBasis.Percentage;
    public decimal CommissionRate { get; set; }
    public CommissionTrigger CommissionTrigger { get; set; } = CommissionTrigger.OnClientPayment;

    protected Agent()
    {
    }

    public Agent(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
