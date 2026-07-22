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
}
