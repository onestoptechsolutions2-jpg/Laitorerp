using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
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

    public ServiceCatalogItemAppService(
        IRepository<ServiceCatalogItem, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;

        GetPolicyName = ErpPermissions.ServiceCatalog.Default;
        GetListPolicyName = ErpPermissions.ServiceCatalog.Default;
        CreatePolicyName = ErpPermissions.ServiceCatalog.Edit;
        UpdatePolicyName = ErpPermissions.ServiceCatalog.Edit;
        DeletePolicyName = ErpPermissions.ServiceCatalog.Edit;
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
        if (userIds.Count == 0)
        {
            return;
        }

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
        entity.TargetSlaHours = input.TargetSlaHours;
        entity.IsActive = input.IsActive;
    }
}
