using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Inventory;

// A stock location. Exactly one row should have IsDefault set (WarehouseAppService enforces it,
// same one-flag-true pattern as TaxRate.IsDefault/Currency.IsBaseCurrency) - new GoodsReceipts/
// Orders default to it so single-location shops never have to think about warehouses at all.
public class Warehouse : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    protected Warehouse()
    {
    }

    public Warehouse(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
