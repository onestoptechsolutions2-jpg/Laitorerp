using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Services.Dtos.Hr;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Hr;

[Mapper]
public partial class EmployeeToEmployeeDtoMapper : MapperBase<Employee, EmployeeDto>
{
    [MapperIgnoreSource(nameof(Employee.ExtraProperties))]
    [MapperIgnoreSource(nameof(Employee.ConcurrencyStamp))]
    public override partial EmployeeDto Map(Employee source);

    [MapperIgnoreSource(nameof(Employee.ExtraProperties))]
    [MapperIgnoreSource(nameof(Employee.ConcurrencyStamp))]
    public override partial void Map(Employee source, EmployeeDto destination);
}
