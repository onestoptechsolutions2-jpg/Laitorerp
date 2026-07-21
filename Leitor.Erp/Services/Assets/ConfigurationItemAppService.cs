using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Assets;

[RequiresFeature(ErpFeatures.AssetManagement)]
public class ConfigurationItemAppService :
    CrudAppService<ConfigurationItem, ConfigurationItemDto, Guid, GetConfigurationItemListInput, CreateUpdateConfigurationItemDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<ConfigurationItemRelationship, Guid> _relationshipRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public ConfigurationItemAppService(
        IRepository<ConfigurationItem, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<ConfigurationItemRelationship, Guid> relationshipRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _relationshipRepository = relationshipRepository;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Assets.Default;
        GetListPolicyName = ErpPermissions.Assets.Default;
        CreatePolicyName = ErpPermissions.Assets.Create;
        UpdatePolicyName = ErpPermissions.Assets.Edit;
        DeletePolicyName = ErpPermissions.Assets.Delete;
    }

    // Relationships are an independent aggregate root with no FK relationship configured, so
    // deleting a CI doesn't cascade automatically - same pattern as every other entity in this app
    // with a child collection.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "ConfigurationItem", id);

        var relationships = await _relationshipRepository.GetListAsync(x => x.SourceCiId == id || x.TargetCiId == id);
        await _relationshipRepository.DeleteManyAsync(relationships);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<ConfigurationItem>> CreateFilteredQueryAsync(GetConfigurationItemListInput input)
    {
        input.Sorting ??= $"{nameof(ConfigurationItem.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!) || (x.SerialNumber != null && x.SerialNumber.Contains(input.Filter!)));
    }

    public override async Task<ConfigurationItemDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ConfigurationItemDto>> GetListAsync(GetConfigurationItemListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ConfigurationItemDto> items)
    {
        var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();
        var namesById = customerIds.Count > 0
            ? (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var item in items)
        {
            if (item.CustomerId.HasValue && namesById.TryGetValue(item.CustomerId.Value, out var customerName))
            {
                item.CustomerName = customerName;
            }
        }
    }

    protected override Task<ConfigurationItem> MapToEntityAsync(CreateUpdateConfigurationItemDto createInput)
    {
        var entity = new ConfigurationItem(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateConfigurationItemDto updateInput, ConfigurationItem entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateConfigurationItemDto input, ConfigurationItem entity)
    {
        entity.Name = input.Name;
        entity.CIType = input.CIType;
        entity.CustomerId = input.CustomerId;
        entity.SerialNumber = input.SerialNumber;
        entity.Status = input.Status;
        entity.PurchaseDate = input.PurchaseDate;
        entity.WarrantyExpiryDate = input.WarrantyExpiryDate;
        entity.Notes = input.Notes;
    }
}
