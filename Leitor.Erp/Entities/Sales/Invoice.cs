using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class Invoice : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }

    // Copied from the source Order.PaymentTerms, or defaulted from Customer.DefaultPaymentTerms
    // for a standalone invoice - purely informational plus the default DueDate suggestion
    // (PaymentTermsCalculator.DueDate); DueDate itself always stays directly editable.
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    // Snapshotted at creation/edit time via CurrencyRateResolver, never recomputed later - same
    // discipline as InvoiceLine.TaxRatePercent.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    protected Invoice()
    {
    }

    public Invoice(Guid id, Guid customerId, string invoiceNumber)
        : base(id)
    {
        CustomerId = customerId;
        InvoiceNumber = invoiceNumber;
    }
}
