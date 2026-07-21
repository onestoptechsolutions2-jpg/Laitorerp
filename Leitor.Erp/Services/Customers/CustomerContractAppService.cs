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

public class CustomerContractAppService :
    CrudAppService<CustomerContract, CustomerContractDto, Guid, GetCustomerContractListInput, CreateUpdateCustomerContractDto>
{
    public CustomerContractAppService(IRepository<CustomerContract, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Edit;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Edit;
    }

    protected override async Task<IQueryable<CustomerContract>> CreateFilteredQueryAsync(GetCustomerContractListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value);
    }

    protected override Task<CustomerContract> MapToEntityAsync(CreateUpdateCustomerContractDto createInput)
    {
        var entity = new CustomerContract(
            GuidGenerator.Create(),
            createInput.CustomerId,
            createInput.ContractNumber,
            createInput.Title
        );
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateCustomerContractDto updateInput, CustomerContract entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateCustomerContractDto input, CustomerContract entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.ContractNumber = input.ContractNumber;
        entity.Title = input.Title;
        entity.Type = input.Type;
        entity.Status = input.Status;
        entity.StartDate = input.StartDate;

        // A changed EndDate is effectively a new crossing to alert on (e.g. a renewal) - clear the
        // stamp so ContractExpiryAlertWorker treats it as unalerted again.
        if (entity.EndDate != input.EndDate)
        {
            entity.LastExpiryAlertSentDate = null;
        }
        entity.EndDate = input.EndDate;

        entity.Value = input.Value;
        entity.Notes = input.Notes;

        entity.SlaUrgentHours = input.SlaUrgentHours;
        entity.SlaHighHours = input.SlaHighHours;
        entity.SlaMediumHours = input.SlaMediumHours;
        entity.SlaLowHours = input.SlaLowHours;
    }
}
