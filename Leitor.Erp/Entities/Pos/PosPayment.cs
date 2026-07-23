using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Pos;

// Split-tender support: a PosSale can have more than one PosPayment row (e.g. part cash, part
// card), same "many payments per document" shape as Entities/Sales/Payment.cs, just reusing its
// PaymentMethod enum rather than defining a duplicate one.
public class PosPayment : FullAuditedAggregateRoot<Guid>
{
    public Guid PosSaleId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Reference { get; set; }

    protected PosPayment()
    {
    }

    public PosPayment(Guid id, Guid posSaleId, decimal amount, PaymentMethod method)
        : base(id)
    {
        PosSaleId = posSaleId;
        Amount = amount;
        Method = method;
    }
}
