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
    WithholdingTaxPayable = 7,
    UnrealizedFxGainLoss = 8,

    // Payroll (Services/Hr/PayrollRunAppService.cs). StatutoryDeductionsPayable is one shared
    // liability role for NSSF/SHA/PAYE/Housing Levy rather than four separate roles - the journal
    // line description distinguishes which statutory body each credit is for, keeping the chart of
    // accounts from growing four single-purpose entries for what's still fundamentally "money owed
    // to the government/statutory funds, not yet remitted."
    SalaryExpense = 9,
    SalaryPayable = 10,
    StatutoryDeductionsPayable = 11
}
