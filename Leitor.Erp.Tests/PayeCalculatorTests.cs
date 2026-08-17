using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Services;
using Xunit;

namespace Leitor.Erp.Tests;

// Pure unit tests, no DB - PayeCalculator is a stateless static class. Bands here are small fixed
// fixtures constructed in the test itself, not the real seeded rates (see
// Data/ErpPayeTaxBandDataSeeder.cs), so this test suite doesn't silently start failing if the
// seeded rates are later corrected to match a Finance Act amendment.
public class PayeCalculatorTests
{
    private static List<PayeTaxBand> ThreeBands()
    {
        var effectiveFrom = new DateTime(2026, 1, 1);
        return new List<PayeTaxBand>
        {
            new(Guid.NewGuid(), 0, 1000m, 10m, effectiveFrom),
            new(Guid.NewGuid(), 1000m, 3000m, 20m, effectiveFrom),
            new(Guid.NewGuid(), 3000m, null, 30m, effectiveFrom)
        };
    }

    [Fact]
    public void ComputeGrossPaye_Income_Entirely_Within_First_Band_Taxes_At_That_Bands_Rate()
    {
        var bands = ThreeBands();

        var tax = PayeCalculator.ComputeGrossPaye(500m, bands);

        Assert.Equal(50m, tax); // 500 * 10%
    }

    [Fact]
    public void ComputeGrossPaye_Income_Spanning_Three_Bands_Accumulates_Correctly()
    {
        var bands = ThreeBands();

        // 1000 @ 10% = 100
        // next 2000 (1000-3000) @ 20% = 400
        // remaining 500 (3000-3500) @ 30% = 150
        // total = 650
        var tax = PayeCalculator.ComputeGrossPaye(3500m, bands);

        Assert.Equal(650m, tax);
    }

    [Fact]
    public void ComputeGrossPaye_Zero_Or_Negative_Income_Returns_Zero()
    {
        var bands = ThreeBands();

        Assert.Equal(0m, PayeCalculator.ComputeGrossPaye(0m, bands));
        Assert.Equal(0m, PayeCalculator.ComputeGrossPaye(-500m, bands));
    }

    [Fact]
    public void ComputeNetPaye_Personal_Relief_Reduces_Tax_But_Never_Below_Zero()
    {
        var bands = ThreeBands();

        // Gross tax on 500 is 50 (10%) - a relief of 200 should floor at zero, not go negative.
        var net = PayeCalculator.ComputeNetPaye(500m, bands, monthlyPersonalRelief: 200m);
        Assert.Equal(0m, net);

        // Gross tax on 3500 is 650 - a relief of 100 should reduce it to 550, not floor.
        var netWithSmallRelief = PayeCalculator.ComputeNetPaye(3500m, bands, monthlyPersonalRelief: 100m);
        Assert.Equal(550m, netWithSmallRelief);
    }
}
