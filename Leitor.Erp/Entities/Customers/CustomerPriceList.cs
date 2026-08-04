using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// Links a Customer to one or more PriceLists, with one marked as primary/default.
// Used throughout the sales flow (Proposal → Quote → Order → Invoice) to determine
// line item pricing unless explicitly overridden.
public class CustomerPriceList : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid PriceListId { get; set; }

    // True if this is the default price list for this customer (auto-selected in proposals)
    public bool IsPrimary { get; set; }

    protected CustomerPriceList()
    {
    }

    public CustomerPriceList(Guid id, Guid customerId, Guid priceListId, bool isPrimary = false)
        : base(id)
    {
        CustomerId = customerId;
        PriceListId = priceListId;
        IsPrimary = isPrimary;
    }
}
