using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Inventory;

// Not a CrudAppService: StockMovement rows are an append-only ledger (see StockMovement's own
// comment) - most are system-generated from GoodsReceiptAppService/OrderAppService, so the only
// user-facing write here is RecordAdjustmentAsync for physical-count corrections. There is no
// Update/Delete - fixing a bad movement means recording an offsetting adjustment, same "never
// mutate history" reasoning as JournalEntryAppService.ReverseAsync.
public class StockMovementAppService : ApplicationService
{
    private readonly IRepository<StockMovement, Guid> _repository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;

    public StockMovementAppService(
        IRepository<StockMovement, Guid> repository,
        IRepository<Product, Guid> productRepository,
        IRepository<Warehouse, Guid> warehouseRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<List<StockMovementDto>> GetListAsync(GetStockMovementListInput input)
    {
        await CheckPolicyAsync(ErpPermissions.Inventory.Default);

        var movements = input.ProductId.HasValue
            ? await _repository.GetListAsync(x => x.ProductId == input.ProductId.Value)
            : await _repository.GetListAsync();

        movements = movements.OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.CreationTime).ToList();

        var dtos = movements.Select(ToDto).ToList();
        await ResolveNamesAsync(dtos);
        return dtos;
    }

    public async Task<StockMovementDto> RecordAdjustmentAsync(RecordStockAdjustmentDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Inventory.Edit);

        var product = await _productRepository.GetAsync(input.ProductId);
        if (!product.TrackInventory)
        {
            throw new UserFriendlyException("This product doesn't have inventory tracking enabled - turn it on from the Catalog page first.");
        }

        var warehouseId = input.WarehouseId
            ?? (await _warehouseRepository.GetListAsync(x => x.IsDefault)).FirstOrDefault()?.Id
            ?? throw new UserFriendlyException("No default warehouse is configured - set one on the Warehouses page first.");

        var movementType = input.IsIncrease ? StockMovementType.AdjustmentIncrease : StockMovementType.AdjustmentDecrease;

        var movement = new StockMovement(GuidGenerator.Create(), input.ProductId, warehouseId, input.MovementDate, 0, movementType);
        var signedQuantity = input.IsIncrease ? Math.Abs(input.Quantity) : -Math.Abs(input.Quantity);
        movement.Quantity = signedQuantity;
        movement.Notes = input.Notes;

        await _repository.InsertAsync(movement, autoSave: true);

        var dto = ToDto(movement);
        await ResolveNamesAsync(new[] { dto });
        return dto;
    }

    private static StockMovementDto ToDto(StockMovement movement)
    {
        return new StockMovementDto
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            WarehouseId = movement.WarehouseId,
            MovementDate = movement.MovementDate,
            Quantity = movement.Quantity,
            MovementType = movement.MovementType,
            SourceDocumentType = movement.SourceDocumentType,
            SourceDocumentId = movement.SourceDocumentId,
            Notes = movement.Notes,
            CreationTime = movement.CreationTime
        };
    }

    private async Task ResolveNamesAsync(IReadOnlyCollection<StockMovementDto> dtos)
    {
        var productIds = dtos.Select(x => x.ProductId).Distinct().ToList();
        var warehouseIds = dtos.Select(x => x.WarehouseId).Distinct().ToList();

        var productsById = productIds.Count > 0
            ? (await _productRepository.GetListAsync(x => productIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();
        var warehousesById = warehouseIds.Count > 0
            ? (await _warehouseRepository.GetListAsync(x => warehouseIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var dto in dtos)
        {
            dto.ProductName = productsById.GetValueOrDefault(dto.ProductId);
            dto.WarehouseName = warehousesById.GetValueOrDefault(dto.WarehouseId);
        }
    }
}
