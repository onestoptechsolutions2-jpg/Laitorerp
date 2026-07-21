namespace Leitor.Erp.Entities.Inventory;

public enum StockMovementType
{
    Receipt = 0,
    Issue = 1,
    AdjustmentIncrease = 2,
    AdjustmentDecrease = 3,
    TransferOut = 4,
    TransferIn = 5
}
