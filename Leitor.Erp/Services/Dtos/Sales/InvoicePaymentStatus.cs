namespace Leitor.Erp.Services.Dtos.Sales;

// Not persisted anywhere - always computed by InvoiceAppService from Payments applied against
// an invoice's line total, the same way Manager.io actually behaves (status is never manually
// set, only derived).
public enum InvoicePaymentStatus
{
    Unpaid = 0,
    Overdue = 1,
    PartiallyPaid = 2,
    PaidInFull = 3,
    Overpaid = 4
}
