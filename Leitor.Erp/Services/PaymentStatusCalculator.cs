using System;
using Leitor.Erp.Entities.Common;
using Leitor.Erp.Services.Dtos.Sales;

namespace Leitor.Erp.Services;

// Centralizes the payment-status formula previously hand-written separately in
// InvoiceAppService.ComputePaymentStatus and SupplierInvoiceAppService.ComputePaymentStatus - one
// rule, computed the same way on both the receivable and payable side, instead of two copies that
// could drift. Computed exactly like Manager.io: never manually set, always derived from payments
// applied.
public static class PaymentStatusCalculator
{
    public static InvoicePaymentStatus Compute(this IPayableDocument document, DateTime now)
    {
        if (document.AmountPaid <= 0)
        {
            return document.DueDate < now ? InvoicePaymentStatus.Overdue : InvoicePaymentStatus.Unpaid;
        }

        if (document.AmountPaid < document.Total)
        {
            return InvoicePaymentStatus.PartiallyPaid;
        }

        return document.AmountPaid > document.Total ? InvoicePaymentStatus.Overpaid : InvoicePaymentStatus.PaidInFull;
    }
}
