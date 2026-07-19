using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

// Direct mirror of Entities/Sales/Payment.cs, on the payable side instead of receivable.
public class VendorPayment : FullAuditedAggregateRoot<Guid>
{
    public Guid SupplierInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    protected VendorPayment()
    {
    }

    public VendorPayment(Guid id, Guid supplierInvoiceId, decimal amount, DateTime paymentDate)
        : base(id)
    {
        SupplierInvoiceId = supplierInvoiceId;
        Amount = amount;
        PaymentDate = paymentDate;
    }
}
