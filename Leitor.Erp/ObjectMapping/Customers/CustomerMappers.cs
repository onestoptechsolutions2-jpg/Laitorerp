using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

// Only the Entity -> Dto direction is Mapperly-generated here. CreateUpdateCustomerDto -> Customer
// isn't, because Customer's Id has a protected setter and requires a constructor argument that
// CreateUpdateCustomerDto has no matching source for (Mapperly can't construct it) - see
// Services/Customers/CustomerAppService.cs for the manual mapping used instead.
[Mapper]
public partial class CustomerToCustomerDtoMapper : MapperBase<Customer, CustomerDto>
{
    [MapperIgnoreSource(nameof(Customer.ExtraProperties))]
    [MapperIgnoreSource(nameof(Customer.ConcurrencyStamp))]
    public override partial CustomerDto Map(Customer source);

    [MapperIgnoreSource(nameof(Customer.ExtraProperties))]
    [MapperIgnoreSource(nameof(Customer.ConcurrencyStamp))]
    public override partial void Map(Customer source, CustomerDto destination);
}
