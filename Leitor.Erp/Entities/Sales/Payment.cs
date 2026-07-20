using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class Payment : FullAuditedAggregateRoot<Guid>
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    // Defaults from the parent Invoice's CurrencyCode at creation but stays independently
    // editable+snapshotted - a real remittance sometimes settles in a different currency than the
    // invoice was billed in.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    protected Payment()
    {
    }

    public Payment(Guid id, Guid invoiceId, decimal amount, DateTime paymentDate)
        : base(id)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        PaymentDate = paymentDate;
    }
}
