using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Hr;

// Kenya NSSF is a simple 2-tier contribution (not a full band ladder like PAYE) - Tier I covers
// pensionable earnings up to the Lower Earnings Limit, Tier II covers the band between the Lower
// and Upper Earnings Limits. Seeded with best-effort current figures (see
// Data/ErpNssfTierDataSeeder.cs) - the Upper Earnings Limit is on a statutory phased-increase
// schedule under the NSSF Act 2013, so this MUST be verified against the current published NSSF
// rate before real payroll use.
public class NssfTier : FullAuditedAggregateRoot<Guid>
{
    public int TierNumber { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public DateTime EffectiveFrom { get; set; }

    protected NssfTier()
    {
    }

    public NssfTier(Guid id, int tierNumber, decimal lowerBound, decimal upperBound, decimal employeeRate, decimal employerRate, DateTime effectiveFrom)
        : base(id)
    {
        TierNumber = tierNumber;
        LowerBound = lowerBound;
        UpperBound = upperBound;
        EmployeeRate = employeeRate;
        EmployerRate = employerRate;
        EffectiveFrom = effectiveFrom;
    }
}
