using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// Sourcing link for dropshipping: which Vendor(s) can supply a Product, at what cost. Cost is the
// vendor's price to Leitor, distinct from Product.UnitPrice (the customer-facing sale price).
// IsPreferred marks the default vendor to pre-fill when creating a Purchase Order from a Sales
// Order - ProductVendorAppService enforces at most one preferred row per Product.
public class ProductVendor : FullAuditedAggregateRoot<Guid>
{
    public Guid ProductId { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorSku { get; set; }
    public decimal Cost { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool IsPreferred { get; set; }
    public string? Notes { get; set; }

    protected ProductVendor()
    {
    }

    public ProductVendor(Guid id, Guid productId, Guid vendorId, decimal cost)
        : base(id)
    {
        ProductId = productId;
        VendorId = vendorId;
        Cost = cost;
    }
}
