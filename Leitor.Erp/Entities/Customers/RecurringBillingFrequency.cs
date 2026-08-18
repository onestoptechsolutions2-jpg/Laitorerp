namespace Leitor.Erp.Entities.Customers;

// Opt-in: None is the default, matching every existing contract's "no automated billing" behavior
// today - only a contract an admin deliberately sets a frequency on gets recurring invoices.
public enum RecurringBillingFrequency
{
    None = 0,
    Monthly = 1,
    Quarterly = 2,
    Annually = 3
}
