namespace Leitor.Erp.Entities.Accounting;

// Marks the handful of accounts JournalPostingService needs to find programmatically when
// auto-posting from Invoices/Payments/SupplierInvoices/VendorPayments - deliberately not a full
// product-or-category-to-account mapping table (that's premature for v1). At most one Account
// should carry a given non-None role - AccountAppService enforces it.
public enum SystemAccountRole
{
    None = 0,
    AccountsReceivable = 1,
    AccountsPayable = 2,
    Cash = 3,
    Revenue = 4,
    Expense = 5,
    Inventory = 6,
    WithholdingTaxPayable = 7
}
