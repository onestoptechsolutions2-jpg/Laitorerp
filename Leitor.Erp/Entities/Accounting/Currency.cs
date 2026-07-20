using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// A small managed lookup table, not a hardcoded enum - currencies in active use can change, and
// exactly one row must be the base/operating currency (CurrencyAppService enforces it, same
// one-flag-true pattern as TaxRate.IsDefault). Every other monetary document references a
// currency by its Code (string), not by this entity's Guid Id - Code is the natural key other
// entities snapshot, matching how TaxRatePercent is snapshotted rather than a live FK lookup.
public class Currency : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; } = true;

    protected Currency()
    {
    }

    public Currency(Guid id, string code, string name, string symbol)
        : base(id)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
    }
}
