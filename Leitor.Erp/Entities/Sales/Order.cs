using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Simplified from eShop's original OrderStatus (Submitted/AwaitingValidation/StockConfirmed/
// Paid/Shipped/Cancelled) since there's no real inventory/shipping concept here - just
// Submitted/Confirmed/Fulfilled/Cancelled.
public class Order : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? QuoteId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    public DateTime OrderDate { get; set; }
    public string? Notes { get; set; }

    // Defaulted from Customer.DefaultPaymentTerms at creation, editable afterwards - drives the
    // suggested due date when this order is converted to an Invoice (see
    // PaymentTermsCalculator.DueDate) and, when Milestone, routes conversion through
    // OrderAppService.ConvertMilestoneToInvoiceAsync instead of the single full-order path.
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

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
