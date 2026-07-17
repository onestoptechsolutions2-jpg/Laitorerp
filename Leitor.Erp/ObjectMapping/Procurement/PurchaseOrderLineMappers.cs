using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class PurchaseOrderLineToPurchaseOrderLineDtoMapper : MapperBase<PurchaseOrderLine, PurchaseOrderLineDto>
{
    [MapperIgnoreSource(nameof(PurchaseOrderLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(PurchaseOrderLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PurchaseOrderLineDto.LineTotal))]
    public override partial PurchaseOrderLineDto Map(PurchaseOrderLine source);

    [MapperIgnoreSource(nameof(PurchaseOrderLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(PurchaseOrderLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PurchaseOrderLineDto.LineTotal))]
    public override partial void Map(PurchaseOrderLine source, PurchaseOrderLineDto destination);
}
