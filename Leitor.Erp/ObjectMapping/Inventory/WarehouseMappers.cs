using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Services.Dtos.Inventory;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Inventory;

[Mapper]
public partial class WarehouseToWarehouseDtoMapper : MapperBase<Warehouse, WarehouseDto>
{
    [MapperIgnoreSource(nameof(Warehouse.ExtraProperties))]
    [MapperIgnoreSource(nameof(Warehouse.ConcurrencyStamp))]
    public override partial WarehouseDto Map(Warehouse source);

    [MapperIgnoreSource(nameof(Warehouse.ExtraProperties))]
    [MapperIgnoreSource(nameof(Warehouse.ConcurrencyStamp))]
    public override partial void Map(Warehouse source, WarehouseDto destination);
}
