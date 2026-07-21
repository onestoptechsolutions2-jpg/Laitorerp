using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

// A small lookup table, not a rules engine - Kenyan VAT categories (standard/zero-rated/exempt)
// change rarely but do change, so this is editable data rather than a hardcoded enum. Exactly one
// row should have IsDefault set (TaxRateAppService enforces it, same pattern as
// ProductVendorAppService enforcing one preferred vendor per product) - that's the rate applied
// when a line has no Product or the Product has no TaxRateId of its own.
public class TaxRate : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool IsDefault { get; set; }

    // Vat (the original, pre-existing meaning of this table) or WithholdingTax (added for Tax
    // Compliance - see Entities/Sales/TaxType.cs). IsDefault is enforced per-type, not globally,
    // so a default VAT rate and a default withholding rate can coexist.
    public TaxType TaxType { get; set; } = TaxType.Vat;

    protected TaxRate()
    {
    }

    public TaxRate(Guid id, string name, decimal percent)
        : base(id)
    {
        Name = name;
        Percent = percent;
    }
}
