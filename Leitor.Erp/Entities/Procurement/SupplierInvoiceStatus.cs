namespace Leitor.Erp.Entities.Procurement;

// Document lifecycle only, mirrors Entities/Sales/InvoiceStatus.cs - amount paid is computed
// separately from VendorPayments, never stored.
public enum SupplierInvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Cancelled = 2
}
