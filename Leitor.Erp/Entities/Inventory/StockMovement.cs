using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Inventory;

// The inventory ledger - a perpetual, append-only record of every quantity change. QuantityOnHand
// is never stored anywhere; it's always summed live from these rows (Quantity is signed: positive
// for Receipt/AdjustmentIncrease/TransferIn, negative for Issue/AdjustmentDecrease/TransferOut) -
// same "compute, never store" discipline as JournalEntryLine balances, and it exists for the same
// reason: a mutable counter can drift out of sync with reality, a ledger can't.
// SourceDocumentType/SourceDocumentId trace a system-generated row back to the GoodsReceipt/Order
// that produced it (null for a manual adjustment) - same shape as JournalEntry's own tracing.
public class StockMovement : FullAuditedAggregateRoot<Guid>
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal Quantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? Notes { get; set; }

    protected StockMovement()
    {
    }

    public StockMovement(Guid id, Guid productId, Guid warehouseId, DateTime movementDate, decimal quantity, StockMovementType movementType)
        : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        MovementDate = movementDate;
        Quantity = quantity;
        MovementType = movementType;
    }
}
