using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class VendorPaymentToVendorPaymentDtoMapper : MapperBase<VendorPayment, VendorPaymentDto>
{
    [MapperIgnoreSource(nameof(VendorPayment.ExtraProperties))]
    [MapperIgnoreSource(nameof(VendorPayment.ConcurrencyStamp))]
    public override partial VendorPaymentDto Map(VendorPayment source);

    [MapperIgnoreSource(nameof(VendorPayment.ExtraProperties))]
    [MapperIgnoreSource(nameof(VendorPayment.ConcurrencyStamp))]
    public override partial void Map(VendorPayment source, VendorPaymentDto destination);
}
