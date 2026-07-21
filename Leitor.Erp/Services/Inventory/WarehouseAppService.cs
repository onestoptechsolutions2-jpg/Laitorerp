using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Inventory;

public class WarehouseAppService :
    CrudAppService<Warehouse, WarehouseDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateWarehouseDto>
{
    public WarehouseAppService(IRepository<Warehouse, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Inventory.Default;
        GetListPolicyName = ErpPermissions.Inventory.Default;
        CreatePolicyName = ErpPermissions.Inventory.Edit;
        UpdatePolicyName = ErpPermissions.Inventory.Edit;
        DeletePolicyName = ErpPermissions.Inventory.Edit;
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
