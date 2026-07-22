namespace Leitor.Erp.Entities.Common;

// Implemented by every document line entity that carries a price/quantity/discount
// (InvoiceLine/OrderLine/QuoteLine/PurchaseOrderLine/SupplierInvoiceLine) so Services/LineMath.cs
// can compute Subtotal once instead of each AppService re-writing the same formula. Existing
// auto-properties already satisfy a get-only interface member, so implementers add no new state.
public interface ILineItem
{
    decimal UnitPrice { get; }
    decimal Quantity { get; }
    decimal DiscountPercent { get; }
}
