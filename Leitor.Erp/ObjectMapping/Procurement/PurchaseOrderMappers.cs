using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class PurchaseOrderToPurchaseOrderDtoMapper : MapperBase<PurchaseOrder, PurchaseOrderDto>
{
    [MapperIgnoreSource(nameof(PurchaseOrder.ExtraProperties))]
    [MapperIgnoreSource(nameof(PurchaseOrder.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PurchaseOrderDto.VendorName))]
    [MapperIgnoreTarget(nameof(PurchaseOrderDto.SourceOrderNumber))]
    [MapperIgnoreTarget(nameof(PurchaseOrderDto.Total))]
    public override partial PurchaseOrderDto Map(PurchaseOrder source);

    [MapperIgnoreSource(nameof(PurchaseOrder.ExtraProperties))]
    [MapperIgnoreSource(nameof(PurchaseOrder.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PurchaseOrderDto.VendorName))]
    [MapperIgnoreTarget(nameof(PurchaseOrderDto.SourceOrderNumber))]
    [MapperIgnoreTarget(nameof(PurchaseOrderDto.Total))]
    public override partial void Map(PurchaseOrder source, PurchaseOrderDto destination);
}
