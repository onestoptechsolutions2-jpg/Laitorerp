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

    // Defaults from the parent SupplierInvoice's CurrencyCode at creation but stays independently
    // editable+snapshotted - same rationale as Entities/Sales/Payment.cs.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    // Snapshotted at creation from the Vendor's own WithholdingTaxRateId (if any) x Amount, never
    // recomputed later - same "snapshot, don't recompute" discipline as every other tax field in
    // this app. Zero for vendors with no withholding rate configured. Reduces the actual Cash paid
    // out without changing the Accounts Payable amount cleared - see VendorPaymentAppService.
    public decimal WithholdingTaxAmount { get; set; }

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
