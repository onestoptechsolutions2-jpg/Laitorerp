using System;

namespace Leitor.Erp.Entities.Common;

// Implemented by every document a Payment/VendorPayment can be applied against
// (InvoiceDto/SupplierInvoiceDto) so Services/PaymentStatusCalculator.cs can compute the
// Unpaid/Overdue/PartiallyPaid/Overpaid/PaidInFull status once instead of each AppService
// re-writing the same rule - same rationale as ILineItem/LineMath.
public interface IPayableDocument
{
    DateTime DueDate { get; }
    decimal Total { get; }
    decimal AmountPaid { get; }
}
