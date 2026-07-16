using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// The sales/service catalog - physical products (hardware) or services (installation labor,
// AMC renewal, etc). Quote/Order/Invoice lines snapshot Description/UnitPrice at the time they're
// added, so deleting or changing a Product never affects historical documents.
public class Product : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public ProductType Type { get; set; } = ProductType.Product;
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;

    protected Product()
    {
    }

    public Product(Guid id, string name, decimal unitPrice)
        : base(id)
    {
        Name = name;
        UnitPrice = unitPrice;
    }
}
