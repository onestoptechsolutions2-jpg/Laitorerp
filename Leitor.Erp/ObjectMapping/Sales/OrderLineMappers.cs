using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class OrderLineToOrderLineDtoMapper : MapperBase<OrderLine, OrderLineDto>
{
    [MapperIgnoreSource(nameof(OrderLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(OrderLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(OrderLineDto.LineTotal))]
    public override partial OrderLineDto Map(OrderLine source);

    [MapperIgnoreSource(nameof(OrderLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(OrderLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(OrderLineDto.LineTotal))]
    public override partial void Map(OrderLine source, OrderLineDto destination);
}
