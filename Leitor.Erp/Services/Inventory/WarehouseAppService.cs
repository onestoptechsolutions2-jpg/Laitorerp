using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Inventory;

public class WarehouseAppService :
    CrudAppService<Warehouse, WarehouseDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateWarehouseDto>
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly IRepository<PosSession, Guid> _posSessionRepository;
    private readonly IRepository<PosSale, Guid> _posSaleRepository;

    public WarehouseAppService(
        IRepository<Warehouse, Guid> repository,
        IRepository<Order, Guid> orderRepository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        IRepository<StockMovement, Guid> stockMovementRepository,
        IRepository<PosSession, Guid> posSessionRepository,
        IRepository<PosSale, Guid> posSaleRepository)
        : base(repository)
    {
        _orderRepository = orderRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _stockMovementRepository = stockMovementRepository;
        _posSessionRepository = posSessionRepository;
        _posSaleRepository = posSaleRepository;

        GetPolicyName = ErpPermissions.Inventory.Default;
        GetListPolicyName = ErpPermissions.Inventory.Default;
        CreatePolicyName = ErpPermissions.Inventory.Edit;
        UpdatePolicyName = ErpPermissions.Inventory.Edit;
        DeletePolicyName = ErpPermissions.Inventory.Edit;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _orderRepository.GetListAsync(x => x.WarehouseId == id)).Count, "Order"),
            (async () => (await _purchaseOrderRepository.GetListAsync(x => x.WarehouseId == id)).Count, "Purchase Order"),
            (async () => (await _goodsReceiptRepository.GetListAsync(x => x.WarehouseId == id)).Count, "Goods Receipt"),
            (async () => (await _stockMovementRepository.GetListAsync(x => x.WarehouseId == id)).Count, "Stock Movement"),
            (async () => (await _posSessionRepository.GetListAsync(x => x.WarehouseId == id)).Count, "POS Session"),
            (async () => (await _posSaleRepository.GetListAsync(x => x.WarehouseId == id)).Count, "POS Sale")
        );

        await Repository.DeleteAsync(id);
    }

    protected override async Task<Warehouse> MapToEntityAsync(CreateUpdateWarehouseDto createInput)
    {
        if (createInput.IsDefault)
        {
            await ClearOtherDefaultsAsync(currentId: null);
        }

        var entity = new Warehouse(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateWarehouseDto updateInput, Warehouse entity)
    {
        if (updateInput.IsDefault)
        {
            await ClearOtherDefaultsAsync(currentId: entity.Id);
        }

        CopyToEntity(updateInput, entity);
    }

    // Keeps "the default warehouse" unambiguous - every new Order/GoodsReceipt falls back to it.
    // Same pattern as TaxRateAppService.ClearOtherDefaultsAsync / CurrencyAppService.
    private async Task ClearOtherDefaultsAsync(Guid? currentId)
    {
        var others = await Repository.GetListAsync(x => x.IsDefault && x.Id != (currentId ?? Guid.Empty));

        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        if (others.Count > 0)
        {
            await Repository.UpdateManyAsync(others);
        }
    }

    private static void CopyToEntity(CreateUpdateWarehouseDto input, Warehouse entity)
    {
        entity.Name = input.Name;
        entity.Address = input.Address;
        entity.IsDefault = input.IsDefault;
        entity.IsActive = input.IsActive;
    }
}
