using System;
using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Common;

namespace Leitor.Erp.Services;

// Centralizes the line-total formula previously hand-written separately in Invoice/Order/Quote/
// PurchaseOrder/SupplierInvoiceAppService, VatReturnAppService, and DashboardAppService - one
// formula, computed the same way everywhere, instead of 16 independent copies that could drift.
public static class LineMath
{
    public static decimal Subtotal(this ILineItem line)
    {
        return line.UnitPrice * line.Quantity * (1 - line.DiscountPercent / 100m);
    }

    public static decimal TaxAmount(this ITaxableLineItem line)
    {
        return line.Subtotal() * line.TaxRatePercent / 100m;
    }

    public static decimal Total(this ITaxableLineItem line)
    {
        return line.Subtotal() + line.TaxAmount();
    }

    // Weighted margin across a whole document's lines: total revenue net of discount (excl. tax)
    // vs total snapshotted Cost, not an average of each line's own margin % - a document with one
    // huge low-margin line and one tiny high-margin line should read as low-margin overall, which
    // a simple average would mask. Shared by QuoteAppService and OrderAppService's margin gates -
    // originally written once in QuoteAppService, extracted here 2026-08-15 when OrderAppService
    // needed the identical calculation. Null (not 0) when there's no revenue to divide by, same
    // "can't compute a meaningful percentage" convention as a 0-UnitPrice line's own MarginPercent.
    public static decimal? MarginPercent<T>(this IEnumerable<T> lines) where T : ICostedLineItem
    {
        var lineList = lines as IReadOnlyCollection<T> ?? lines.ToList();
        var revenue = lineList.Sum(x => x.Subtotal());
        if (revenue <= 0)
        {
            return null;
        }

        var cost = lineList.Sum(x => x.Cost * x.Quantity);
        return Math.Round(100m * (revenue - cost) / revenue, 1);
    }
}
