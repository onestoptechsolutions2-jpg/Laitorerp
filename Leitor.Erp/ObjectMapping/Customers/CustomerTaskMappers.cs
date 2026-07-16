using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class CustomerTaskToCustomerTaskDtoMapper : MapperBase<CustomerTask, CustomerTaskDto>
{
    [MapperIgnoreSource(nameof(CustomerTask.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerTask.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CustomerTaskDto.AssignedToUserName))]
    public override partial CustomerTaskDto Map(CustomerTask source);

    [MapperIgnoreSource(nameof(CustomerTask.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerTask.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CustomerTaskDto.AssignedToUserName))]
    public override partial void Map(CustomerTask source, CustomerTaskDto destination);
}
