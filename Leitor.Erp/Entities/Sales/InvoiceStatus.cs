namespace Leitor.Erp.Entities.Sales;

// Document lifecycle only - payment status (Unpaid/PartiallyPaid/PaidInFull/Overpaid/Overdue) is
// computed separately from Payments, never stored (see InvoiceAppService), matching how
// Manager.io actually behaves.
public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Cancelled = 2
}
