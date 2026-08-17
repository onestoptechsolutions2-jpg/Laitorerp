using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Hr;

// Kenya PAYE (income tax) progressive band - see Services/PayeCalculator.cs for the computation.
// A database table rather than a hardcoded constant array, deliberately: Finance Act amendments
// change these periodically, and an admin should be able to correct/update a rate without a code
// deploy - same reasoning ErpTaxRateDataSeeder/TaxRate already established for VAT rates. Seeded
// with best-effort current rates (see Data/ErpPayeTaxBandDataSeeder.cs) - MUST be verified against
// the current published KRA PAYE table before this is used for a real payroll run.
public class PayeTaxBand : FullAuditedAggregateRoot<Guid>
{
    public decimal LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }

    protected PayeTaxBand()
    {
    }

    public PayeTaxBand(Guid id, decimal lowerBound, decimal? upperBound, decimal rate, DateTime effectiveFrom)
        : base(id)
    {
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Rate = rate;
        EffectiveFrom = effectiveFrom;
    }
}
