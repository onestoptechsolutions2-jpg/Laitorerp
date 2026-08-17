using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Hr;

[RequiresFeature(ErpFeatures.HumanResources)]
public class EmployeeAppService :
    CrudAppService<Employee, EmployeeDto, Guid, GetEmployeeListInput, CreateUpdateEmployeeDto>
{
    public EmployeeAppService(IRepository<Employee, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Employees.Default;
        GetListPolicyName = ErpPermissions.Employees.Default;
        CreatePolicyName = ErpPermissions.Employees.Create;
        UpdatePolicyName = ErpPermissions.Employees.Edit;
        DeletePolicyName = ErpPermissions.Employees.Delete;
    }

    protected override async Task<IQueryable<Employee>> CreateFilteredQueryAsync(GetEmployeeListInput input)
    {
        input.Sorting ??= $"{nameof(Employee.FullName)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.FullName.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value);
    }

    protected override Task<Employee> MapToEntityAsync(CreateUpdateEmployeeDto createInput)
    {
        var entity = new Employee(GuidGenerator.Create(), createInput.FullName, createInput.HireDate);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateEmployeeDto updateInput, Employee entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateEmployeeDto input, Employee entity)
    {
        entity.FullName = input.FullName;
        entity.Email = input.Email;
        entity.Phone = input.Phone;
        entity.UserId = input.UserId;
        entity.JobTitle = input.JobTitle;
        entity.Department = input.Department;
        entity.HireDate = input.HireDate;
        entity.TerminationDate = input.TerminationDate;
        entity.IsActive = input.IsActive;
        entity.KraPin = input.KraPin;
        entity.NssfNumber = input.NssfNumber;
        entity.ShaNumber = input.ShaNumber;
        entity.BasicSalary = input.BasicSalary;
        entity.BankName = input.BankName;
        entity.BankAccountNumber = input.BankAccountNumber;
    }
}
