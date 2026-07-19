using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// A customer-specific default-price suggestion, not a pricing-enforcement engine - see
// PriceListItem and Customer.DefaultPriceListId. The line's UnitPrice is always still a plain
// editable field; this only changes what number the Add Line form suggests.
public class PriceList : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;

    protected PriceList()
    {
    }

    public PriceList(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
