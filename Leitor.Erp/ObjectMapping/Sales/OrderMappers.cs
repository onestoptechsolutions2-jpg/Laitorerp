using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class OrderToOrderDtoMapper : MapperBase<Order, OrderDto>
{
    [MapperIgnoreSource(nameof(Order.ExtraProperties))]
    [MapperIgnoreSource(nameof(Order.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(OrderDto.CustomerName))]
    [MapperIgnoreTarget(nameof(OrderDto.Total))]
    public override partial OrderDto Map(Order source);

    [MapperIgnoreSource(nameof(Order.ExtraProperties))]
    [MapperIgnoreSource(nameof(Order.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(OrderDto.CustomerName))]
    [MapperIgnoreTarget(nameof(OrderDto.Total))]
    public override partial void Map(Order source, OrderDto destination);
}
