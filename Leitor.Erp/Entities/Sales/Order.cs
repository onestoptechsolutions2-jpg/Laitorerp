using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Simplified from eShop's original OrderStatus (Submitted/AwaitingValidation/StockConfirmed/
// Paid/Shipped/Cancelled) down to Submitted/Confirmed/Fulfilled/Cancelled - the transition into
// Fulfilled is also the moment OrderAppService auto-posts a stock Issue movement per line (see
// WarehouseId below).
public class Order : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? QuoteId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    public DateTime OrderDate { get; set; }
    public string? Notes { get; set; }

    // Which stock location fulfills this order - defaults to the warehouse flagged IsDefault.
    // Only consulted for lines whose Product has TrackInventory set.
    public Guid WarehouseId { get; set; }

    // Defaulted from Customer.DefaultPaymentTerms at creation, editable afterwards - drives the
    // suggested due date when this order is converted to an Invoice (see
    // PaymentTermsCalculator.DueDate) and, when Milestone, routes conversion through
    // OrderAppService.ConvertMilestoneToInvoiceAsync instead of the single full-order path.
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    public int Version { get; set; } = 1;

    // Snapshotted at creation/edit time via CurrencyRateResolver, never recomputed later - same
    // discipline as OrderLine.TaxRatePercent.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    // Locked once it leaves Submitted (Confirmed/Fulfilled/Cancelled) - same lock/single-use-
    // unlock mechanism as Quote.IsLocked/Proposal.IsLocked, enforced in
    // OrderAppService.MapToEntityAsync.
    public bool IsLocked => Status != OrderStatus.Submitted;

    public Guid? UnlockedByUserId { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }

    protected Order()
    {
    }

    public Order(Guid id, Guid customerId, string orderNumber)
        : base(id)
    {
        CustomerId = customerId;
        OrderNumber = orderNumber;
    }
}
