using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.ServiceCatalog;

[RequiresFeature(ErpFeatures.ServiceCatalog)]
public class ServiceCatalogItemAppService :
    CrudAppService<ServiceCatalogItem, ServiceCatalogItemDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateServiceCatalogItemDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<ServiceRequest, Guid> _serviceRequestRepository;
    private readonly IRepository<Partner, Guid> _partnerRepository;

    public ServiceCatalogItemAppService(
        IRepository<ServiceCatalogItem, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<ServiceRequest, Guid> serviceRequestRepository,
        IRepository<Partner, Guid> partnerRepository)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;
        _serviceRequestRepository = serviceRequestRepository;
        _partnerRepository = partnerRepository;

        GetPolicyName = ErpPermissions.ServiceCatalog.Default;
        GetListPolicyName = ErpPermissions.ServiceCatalog.Default;
        CreatePolicyName = ErpPermissions.ServiceCatalog.Edit;
        UpdatePolicyName = ErpPermissions.ServiceCatalog.Edit;
        DeletePolicyName = ErpPermissions.ServiceCatalog.Edit;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _serviceRequestRepository.GetListAsync(x => x.ServiceCatalogItemId == id)).Count, "Service Request")
        );

        await Repository.DeleteAsync(id);
    }

    public override async Task<ServiceCatalogItemDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveOwnerNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ServiceCatalogItemDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var result = await base.GetListAsync(input);
        await ResolveOwnerNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveOwnerNamesAsync(IReadOnlyCollection<ServiceCatalogItemDto> items)
    {
        var userIds = items.Where(x => x.OwnerUserId.HasValue).Select(x => x.OwnerUserId!.Value).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
            var namesById = users.ToDictionary(x => x.Id, x => x.UserName);
            foreach (var item in items)
            {
                if (item.OwnerUserId.HasValue && namesById.TryGetValue(item.OwnerUserId.Value, out var userName))
                {
                    item.OwnerUserName = userName;
                }
            }
        }

        var partnerIds = items.Where(x => x.PartnerId.HasValue).Select(x => x.PartnerId!.Value).Distinct().ToList();
        if (partnerIds.Count > 0)
        {
            var partnerNamesById = (await _partnerRepository.GetListAsync(x => partnerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);
            foreach (var item in items)
            {
                if (item.PartnerId.HasValue && partnerNamesById.TryGetValue(item.PartnerId.Value, out var partnerName))
                {
                    item.PartnerName = partnerName;
                }
            }
        }
    }

    protected override Task<ServiceCatalogItem> MapToEntityAsync(CreateUpdateServiceCatalogItemDto createInput)
    {
        var entity = new ServiceCatalogItem(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateServiceCatalogItemDto updateInput, ServiceCatalogItem entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateServiceCatalogItemDto input, ServiceCatalogItem entity)
    {
        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.Category = input.Category;
        entity.OwnerUserId = input.OwnerUserId;
        entity.PartnerId = input.PartnerId;
        entity.TargetSlaHours = input.TargetSlaHours;
        entity.IsActive = input.IsActive;
    }
}
