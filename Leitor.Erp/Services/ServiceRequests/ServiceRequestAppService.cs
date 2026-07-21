using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.ServiceRequests;

[RequiresFeature(ErpFeatures.ServiceRequestManagement)]
public class ServiceRequestAppService :
    CrudAppService<ServiceRequest, ServiceRequestDto, Guid, GetServiceRequestListInput, CreateUpdateServiceRequestDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<ServiceCatalogItem, Guid> _serviceCatalogItemRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IClock _clock;
    private readonly IDataFilter _dataFilter;

    public ServiceRequestAppService(
        IRepository<ServiceRequest, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<ServiceCatalogItem, Guid> serviceCatalogItemRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IClock clock,
        IDataFilter dataFilter)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _serviceCatalogItemRepository = serviceCatalogItemRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _clock = clock;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.ServiceRequests.Default;
        GetListPolicyName = ErpPermissions.ServiceRequests.Default;
        CreatePolicyName = ErpPermissions.ServiceRequests.Create;
        UpdatePolicyName = ErpPermissions.ServiceRequests.Edit;
        DeletePolicyName = ErpPermissions.ServiceRequests.Delete;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "ServiceRequest", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<ServiceRequest>> CreateFilteredQueryAsync(GetServiceRequestListInput input)
    {
        input.Sorting ??= $"{nameof(ServiceRequest.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Description.Contains(input.Filter!) || x.RequestNumber.Contains(input.Filter!));
    }

    public override async Task<ServiceRequestDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ServiceRequestDto>> GetListAsync(GetServiceRequestListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ServiceRequestDto> requests)
    {
        var customerIds = requests.Select(x => x.CustomerId).Distinct().ToList();
        var namesById = customerIds.Count > 0
            ? (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var catalogItemIds = requests.Where(x => x.ServiceCatalogItemId.HasValue).Select(x => x.ServiceCatalogItemId!.Value).Distinct().ToList();
        var catalogNamesById = catalogItemIds.Count > 0
            ? (await _serviceCatalogItemRepository.GetListAsync(x => catalogItemIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var request in requests)
        {
            if (namesById.TryGetValue(request.CustomerId, out var customerName))
            {
                request.CustomerName = customerName;
            }

            if (request.ServiceCatalogItemId.HasValue && catalogNamesById.TryGetValue(request.ServiceCatalogItemId.Value, out var catalogName))
            {
                request.ServiceCatalogItemName = catalogName;
            }
        }
    }

    protected override async Task<ServiceRequest> MapToEntityAsync(CreateUpdateServiceRequestDto createInput)
    {
        var requestNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "SR-");

        var entity = new ServiceRequest(GuidGenerator.Create(), requestNumber, createInput.CustomerId, createInput.Description);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateServiceRequestDto updateInput, ServiceRequest entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateServiceRequestDto input, ServiceRequest entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.ServiceCatalogItemId = input.ServiceCatalogItemId;
        entity.Description = input.Description;
        entity.RequestedDate = input.RequestedDate;

        var wasTerminal = entity.Status is ServiceRequestStatus.Fulfilled or ServiceRequestStatus.Rejected;
        var isTerminal = input.Status is ServiceRequestStatus.Fulfilled or ServiceRequestStatus.Rejected;

        if (isTerminal && !wasTerminal)
        {
            entity.FulfilledDate = _clock.Now;
        }
        else if (!isTerminal)
        {
            entity.FulfilledDate = null;
        }

        entity.Status = input.Status;
    }
}
