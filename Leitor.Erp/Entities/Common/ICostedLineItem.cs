namespace Leitor.Erp.Entities.Common;

// Implemented by QuoteLine/OrderLine - the two line types that snapshot a per-unit Cost for
// internal margin calculation (see LineMath.MarginPercent). PurchaseOrderLine/SupplierInvoiceLine/
// InvoiceLine don't: a Cost on those would mean something different (what we owe a vendor, or a
// margin computed after the sale already happened), so they deliberately don't implement this.
public interface ICostedLineItem : ILineItem
{
    decimal Cost { get; }
}
