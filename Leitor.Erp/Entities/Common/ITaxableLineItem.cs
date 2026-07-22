namespace Leitor.Erp.Entities.Common;

// Only InvoiceLine/OrderLine/QuoteLine implement this - PurchaseOrderLine/SupplierInvoiceLine
// have never captured a per-line tax rate (see Services/Tax/VatReturnAppService.cs's own
// documented scope cut), so they stop at ILineItem and only ever need Subtotal(), never Total().
public interface ITaxableLineItem : ILineItem
{
    decimal TaxRatePercent { get; }
}
