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

public class CustomerAppService :
    CrudAppService<Customer, CustomerDto, Guid, GetCustomerListInput, CreateUpdateCustomerDto>
{
    private readonly IRepository<CustomerContact, Guid> _contactRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;

    public CustomerAppService(
        IRepository<Customer, Guid> repository,
        IRepository<CustomerContact, Guid> contactRepository,
        IRepository<CustomerContract, Guid> contractRepository)
        : base(repository)
    {
        _contactRepository = contactRepository;
        _contractRepository = contractRepository;

        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Create;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Delete;
    }

    // Contacts and contracts are independent aggregate roots (see CustomerContact.cs /
    // CustomerContract.cs comments), so deleting a customer doesn't cascade automatically -
    // there's no FK relationship configured in ErpDbContext, just an index on CustomerId.
    // Cascade the soft-delete explicitly here so nothing is left orphaned/unreachable.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        var contacts = await _contactRepository.GetListAsync(x => x.CustomerId == id);
        await _contactRepository.DeleteManyAsync(contacts);

        var contracts = await _contractRepository.GetListAsync(x => x.CustomerId == id);
        await _contractRepository.DeleteManyAsync(contracts);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Customer>> CreateFilteredQueryAsync(GetCustomerListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(
            !string.IsNullOrWhiteSpace(input.Filter),
            x => x.Name.Contains(input.Filter!) ||
                 (x.Email != null && x.Email.Contains(input.Filter!))
        );
    }

    // CreateUpdateCustomerDto -> Customer is mapped manually rather than via Mapperly: Customer's
    // Id has a protected setter and its constructor requires a generated Guid, which Mapperly
    // cannot resolve from the DTO. See ObjectMapping/Customers/CustomerMappers.cs for the (safe)
    // Entity -> Dto direction, which Mapperly does handle.
    protected override Task<Customer> MapToEntityAsync(CreateUpdateCustomerDto createInput)
    {
        var entity = new Customer(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateCustomerDto updateInput, Customer entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateCustomerDto input, Customer entity)
    {
        entity.Name = input.Name;
        entity.Email = input.Email;
        entity.PhoneNumber = input.PhoneNumber;
        entity.AddressLine = input.AddressLine;
        entity.City = input.City;
        entity.State = input.State;
        entity.PostalCode = input.PostalCode;
        entity.Country = input.Country;
        entity.Status = input.Status;
        entity.Notes = input.Notes;
    }
}
