using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Customers;

public class CustomerContactAppService :
    CrudAppService<CustomerContact, CustomerContactDto, Guid, GetCustomerContactListInput, CreateUpdateCustomerContactDto>
{
    public CustomerContactAppService(IRepository<CustomerContact, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Edit;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Edit;
    }

    protected override async Task<IQueryable<CustomerContact>> CreateFilteredQueryAsync(GetCustomerContactListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value);
    }

    protected override Task<CustomerContact> MapToEntityAsync(CreateUpdateCustomerContactDto createInput)
    {
        var entity = new CustomerContact(GuidGenerator.Create(), createInput.CustomerId, createInput.FullName);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateCustomerContactDto updateInput, CustomerContact entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateCustomerContactDto input, CustomerContact entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.FullName = input.FullName;
        entity.JobTitle = input.JobTitle;
        entity.Email = input.Email;
        entity.PhoneNumber = input.PhoneNumber;
        entity.IsPrimary = input.IsPrimary;
        entity.Notes = input.Notes;
    }
}
