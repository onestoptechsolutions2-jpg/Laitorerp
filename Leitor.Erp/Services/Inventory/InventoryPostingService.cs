using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Services.Inventory;

// Called from GoodsReceiptAppService/OrderAppService to auto-post a StockMovement the moment
// stock physically moves. Same static-method-with-injected-deps shape as
// JournalPostingService/WorkflowStageLog/DeletionGate, so callers don't need a dependency on a
// stateful service just to append one ledger row. Deliberately doesn't guard against a negative
// resulting balance on an Issue - an oversold product is a real, visible signal on the Stock on
// Hand report, not something this layer should silently block Order fulfillment over.
public static class InventoryPostingService
{
    public static class SourceDocumentTypes
    {
        public const string GoodsReceipt = "GoodsReceipt";
        public const string Order = "Order";
    }

    public static async Task PostAsync(
        IRepository<StockMovement, Guid> stockMovementRepository,
        IGuidGenerator guidGenerator,
        Guid productId,
        Guid warehouseId,
        DateTime movementDate,
        decimal quantity,
        StockMovementType movementType,
        string? sourceDocumentType = null,
        Guid? sourceDocumentId = null,
        string? notes = null)
    {
        if (quantity == 0)
        {
            return;
        }

        var isOutbound = movementType is StockMovementType.Issue or StockMovementType.AdjustmentDecrease or StockMovementType.TransferOut;
        var signedQuantity = isOutbound ? -Math.Abs(quantity) : Math.Abs(quantity);

        var movement = new StockMovement(guidGenerator.Create(), productId, warehouseId, movementDate, signedQuantity, movementType)
        {
            SourceDocumentType = sourceDocumentType,
            SourceDocumentId = sourceDocumentId,
            Notes = notes
        };

        await stockMovementRepository.InsertAsync(movement, autoSave: true);
    }
}
