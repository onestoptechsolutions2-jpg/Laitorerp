using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.FieldService;

// Lightweight version of ServiceNow FSM's Stockroom/parts-consumption tracking - records what was
// used on a visit. ProductId is nullable (same snapshot pattern as Entities/Sales/QuoteLine.cs):
// references the Sales module's Product catalog when possible, but doesn't require every part to
// be catalogued. Not wired into automatic invoice-line generation - a natural extension point
// later, not built now.
public class FieldServiceJobPart : FullAuditedAggregateRoot<Guid>
{
    public Guid JobId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;

    protected FieldServiceJobPart()
    {
    }

    public FieldServiceJobPart(Guid id, Guid jobId, string description)
        : base(id)
    {
        JobId = jobId;
        Description = description;
    }
}
