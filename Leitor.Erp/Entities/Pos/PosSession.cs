using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Pos;

// A till/register session - PosSaleAppService enforces at most one Open session per Warehouse at
// a time, so every completed sale can unambiguously be attributed to a session. ClosingCashAmount
// is the amount physically counted at close, compared against the session's own computed expected
// cash total (opening + cash-tender sales) on the Close screen - a discrepancy is shown, not
// blocked, since a till can legitimately be short/over.
public class PosSession : FullAuditedAggregateRoot<Guid>
{
    public Guid WarehouseId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public PosSessionStatus Status { get; set; } = PosSessionStatus.Open;
    public Guid? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal? ClosingCashAmount { get; set; }

    protected PosSession()
    {
    }

    public PosSession(Guid id, Guid warehouseId, Guid openedByUserId, DateTime openedAt, decimal openingCashAmount)
        : base(id)
    {
        WarehouseId = warehouseId;
        OpenedByUserId = openedByUserId;
        OpenedAt = openedAt;
        OpeningCashAmount = openingCashAmount;
    }
}
