using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class CustomerContactToCustomerContactDtoMapper : MapperBase<CustomerContact, CustomerContactDto>
{
    [MapperIgnoreSource(nameof(CustomerContact.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerContact.ConcurrencyStamp))]
    public override partial CustomerContactDto Map(CustomerContact source);

    [MapperIgnoreSource(nameof(CustomerContact.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerContact.ConcurrencyStamp))]
    public override partial void Map(CustomerContact source, CustomerContactDto destination);
}
