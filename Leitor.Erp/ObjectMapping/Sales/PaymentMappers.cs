using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class PaymentToPaymentDtoMapper : MapperBase<Payment, PaymentDto>
{
    [MapperIgnoreSource(nameof(Payment.ExtraProperties))]
    [MapperIgnoreSource(nameof(Payment.ConcurrencyStamp))]
    public override partial PaymentDto Map(Payment source);

    [MapperIgnoreSource(nameof(Payment.ExtraProperties))]
    [MapperIgnoreSource(nameof(Payment.ConcurrencyStamp))]
    public override partial void Map(Payment source, PaymentDto destination);
}
