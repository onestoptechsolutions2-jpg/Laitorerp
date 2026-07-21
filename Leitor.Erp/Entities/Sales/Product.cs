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
    public Guid? CategoryId { get; set; }

    // A bundle explodes into one QuoteLine/OrderLine per ProductBundleItem when added to a
    // document (see QuoteLineAppService/OrderLineAppService.CreateAsync), rather than being sold
    // as a single opaque line - serves "installation packages" (hardware + labor sold together but
    // itemized on the actual document).
    public bool IsBundle { get; set; }

    // Standard/default cost used for margin calc on Quote/Order lines when there's no
    // ProductVendor row yet - distinct from ProductVendor.Cost (a specific vendor's price).
    public decimal Cost { get; set; }

    // The rate this product normally carries; null falls back to whichever TaxRate has
    // IsDefault set. Snapshotted onto each line at add-time (see QuoteLine.TaxRatePercent) so a
    // later edit here never changes an already-issued document.
    public Guid? TaxRateId { get; set; }

    // Services never carry stock - TrackInventory defaults true only for Type == Product, and can
    // still be turned off per-item (e.g. a non-stock/drop-ship product). ReorderPoint/
    // ReorderQuantity are only meaningful when this is true; StockMovement quantities are never
    // touched for a product with TrackInventory == false.
    public bool TrackInventory { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }

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
