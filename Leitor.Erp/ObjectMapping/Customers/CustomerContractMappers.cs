using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class CustomerContractToCustomerContractDtoMapper : MapperBase<CustomerContract, CustomerContractDto>
{
    [MapperIgnoreSource(nameof(CustomerContract.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerContract.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(CustomerContract.LastExpiryAlertSentDate))]
    public override partial CustomerContractDto Map(CustomerContract source);

    [MapperIgnoreSource(nameof(CustomerContract.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerContract.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(CustomerContract.LastExpiryAlertSentDate))]
    public override partial void Map(CustomerContract source, CustomerContractDto destination);
}
