using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Customers;

public class CustomerContractAppService :
    CrudAppService<CustomerContract, CustomerContractDto, Guid, GetCustomerContractListInput, CreateUpdateCustomerContractDto>
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<WarrantyClaim, Guid> _warrantyClaimRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<Project, Guid> _projectRepository;

    public CustomerContractAppService(
        IRepository<CustomerContract, Guid> repository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<WarrantyClaim, Guid> warrantyClaimRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<Project, Guid> projectRepository)
        : base(repository)
    {
        _ticketRepository = ticketRepository;
        _warrantyClaimRepository = warrantyClaimRepository;
        _jobRepository = jobRepository;
        _projectRepository = projectRepository;

        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Edit;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Edit;
    }

    // No independent Index/Detail pages for CustomerContract (managed only from Customer Detail),
    // but Ticket/WarrantyClaim/FieldServiceJob/Project can all still reference one after it's
    // created - block rather than leave those with a dangling ContractId/ConvertedToContractId.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _ticketRepository.GetListAsync(x => x.ContractId == id)).Count, "Ticket"),
            (async () => (await _warrantyClaimRepository.GetListAsync(x => x.ContractId == id)).Count, "Warranty Claim"),
            (async () => (await _jobRepository.GetListAsync(x => x.ContractId == id)).Count, "Field Service Job"),
            (async () => (await _projectRepository.GetListAsync(x => x.ConvertedToContractId == id)).Count, "Project")
        );

        await Repository.DeleteAsync(id);
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

        entity.ServicesIncluded = input.ServicesIncluded;
    }
}
