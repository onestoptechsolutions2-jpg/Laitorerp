using System;
using System.Collections.Generic;
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
using Volo.Abp.Application.Dtos;
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
    private readonly IRepository<ContractTemplate, Guid> _contractTemplateRepository;

    public CustomerContractAppService(
        IRepository<CustomerContract, Guid> repository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<WarrantyClaim, Guid> warrantyClaimRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<Project, Guid> projectRepository,
        IRepository<ContractTemplate, Guid> contractTemplateRepository)
        : base(repository)
    {
        _ticketRepository = ticketRepository;
        _warrantyClaimRepository = warrantyClaimRepository;
        _jobRepository = jobRepository;
        _projectRepository = projectRepository;
        _contractTemplateRepository = contractTemplateRepository;

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

    public override async Task<CustomerContractDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<CustomerContractDto>> GetListAsync(GetCustomerContractListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<CustomerContractDto> contracts)
    {
        var templateIds = contracts
            .Where(x => x.ContractTemplateId.HasValue)
            .Select(x => x.ContractTemplateId!.Value)
            .Distinct()
            .ToList();
        var templateNamesById = templateIds.Count > 0
            ? (await _contractTemplateRepository.GetListAsync(x => templateIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var contract in contracts)
        {
            if (contract.ContractTemplateId.HasValue && templateNamesById.TryGetValue(contract.ContractTemplateId.Value, out var templateName))
            {
                contract.ContractTemplateName = templateName;
            }
        }
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

        entity.ContractTemplateId = input.ContractTemplateId;
        entity.ClientSignatoryName = input.ClientSignatoryName;

        // Defaults NextBillingDate the first time billing is turned on for this contract, so an
        // admin enabling it doesn't also have to compute a starting date by hand. Left alone on
        // every later save (including a manual override) unless billing is turned back off.
        var turningOnBilling = input.BillingFrequency != RecurringBillingFrequency.None && entity.BillingFrequency == RecurringBillingFrequency.None;
        entity.BillingFrequency = input.BillingFrequency;

        if (input.BillingFrequency == RecurringBillingFrequency.None)
        {
            entity.NextBillingDate = null;
        }
        else if (input.NextBillingDate.HasValue)
        {
            entity.NextBillingDate = input.NextBillingDate;
        }
        else if (turningOnBilling)
        {
            entity.NextBillingDate = entity.StartDate;
        }
    }
}
