using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Hr;

namespace Leitor.Erp.Services;

// First-of-its-kind progressive-band calculator in this codebase - no existing "Bracket"/"Tier"
// calculation pattern to copy the shape from (ChangeTier is an approval-routing enum, not a
// monetary calculation). Style-matched to LineMath.cs's static-pure-function convention.
public static class PayeCalculator
{
    // Iterates bands ordered by LowerBound ascending, taxing only the slice of income that falls
    // within each band (the standard progressive-tax accumulation, not "highest band rate applied
    // to the whole income").
    public static decimal ComputeGrossPaye(decimal taxableIncome, IReadOnlyList<PayeTaxBand> bands)
    {
        if (taxableIncome <= 0)
        {
            return 0m;
        }

        decimal totalTax = 0m;
        foreach (var band in bands.OrderBy(x => x.LowerBound))
        {
            if (taxableIncome <= band.LowerBound)
            {
                break;
            }

            var bandCeiling = band.UpperBound ?? decimal.MaxValue;
            var incomeInBand = System.Math.Min(taxableIncome, bandCeiling) - band.LowerBound;
            if (incomeInBand <= 0)
            {
                continue;
            }

            totalTax += incomeInBand * (band.Rate / 100m);
        }

        return totalTax;
    }

    // Personal relief reduces the gross tax computed above but never below zero.
    public static decimal ComputeNetPaye(decimal taxableIncome, IReadOnlyList<PayeTaxBand> bands, decimal monthlyPersonalRelief)
    {
        var grossPaye = ComputeGrossPaye(taxableIncome, bands);
        var netPaye = grossPaye - monthlyPersonalRelief;
        return netPaye < 0 ? 0m : netPaye;
    }
}
