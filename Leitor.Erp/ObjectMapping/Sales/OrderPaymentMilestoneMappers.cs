using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class OrderPaymentMilestoneToOrderPaymentMilestoneDtoMapper : MapperBase<OrderPaymentMilestone, OrderPaymentMilestoneDto>
{
    [MapperIgnoreSource(nameof(OrderPaymentMilestone.ExtraProperties))]
    [MapperIgnoreSource(nameof(OrderPaymentMilestone.ConcurrencyStamp))]
    public override partial OrderPaymentMilestoneDto Map(OrderPaymentMilestone source);

    [MapperIgnoreSource(nameof(OrderPaymentMilestone.ExtraProperties))]
    [MapperIgnoreSource(nameof(OrderPaymentMilestone.ConcurrencyStamp))]
    public override partial void Map(OrderPaymentMilestone source, OrderPaymentMilestoneDto destination);
}
