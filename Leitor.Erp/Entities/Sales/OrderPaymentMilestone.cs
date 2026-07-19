using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Only meaningful when the owning Order.PaymentTerms == Milestone - one row per planned partial
// invoice (e.g. "Deposit" 30%, "On Delivery" 50%, "Final" 20%). Independent aggregate root, same
// no-DB-FK convention as OrderLine. IsInvoiced/InvoiceId are set once
// OrderAppService.ConvertMilestoneToInvoiceAsync has billed this milestone, so it isn't billed
// twice.
public class OrderPaymentMilestone : FullAuditedAggregateRoot<Guid>
{
    public Guid OrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool IsInvoiced { get; set; }
    public Guid? InvoiceId { get; set; }

    protected OrderPaymentMilestone()
    {
    }

    public OrderPaymentMilestone(Guid id, Guid orderId, string description, decimal percent)
        : base(id)
    {
        OrderId = orderId;
        Description = description;
        Percent = percent;
    }
}
