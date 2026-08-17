using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class PriceListItemAppService :
    CrudAppService<PriceListItem, PriceListItemDto, Guid, GetPriceListItemListInput, CreateUpdatePriceListItemDto>
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ServiceCatalogItem, Guid> _serviceCatalogItemRepository;

    public PriceListItemAppService(
        IRepository<PriceListItem, Guid> repository,
        IRepository<Product, Guid> productRepository,
        IRepository<ServiceCatalogItem, Guid> serviceCatalogItemRepository)
        : base(repository)
    {
        _productRepository = productRepository;
        _serviceCatalogItemRepository = serviceCatalogItemRepository;

        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Edit;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Edit;
    }

    protected override async Task<IQueryable<PriceListItem>> CreateFilteredQueryAsync(GetPriceListItemListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.PriceListId.HasValue, x => x.PriceListId == input.PriceListId!.Value)
            .WhereIf(input.ProductId.HasValue, x => x.ProductId == input.ProductId!.Value);
    }

    public override async Task<PriceListItemDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<PriceListItemDto>> GetListAsync(GetPriceListItemListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveNamesAsync(IReadOnlyCollection<PriceListItemDto> items)
    {
        var productIds = items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToList();
        if (productIds.Count > 0)
        {
            var namesById = (await _productRepository.GetListAsync(x => productIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);
            foreach (var item in items)
            {
                if (item.ProductId.HasValue && namesById.TryGetValue(item.ProductId.Value, out var productName))
                {
                    item.ProductName = productName;
                }
            }
        }

        var serviceIds = items.Where(x => x.ServiceCatalogItemId.HasValue).Select(x => x.ServiceCatalogItemId!.Value).Distinct().ToList();
        if (serviceIds.Count > 0)
        {
            var namesById = (await _serviceCatalogItemRepository.GetListAsync(x => serviceIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);
            foreach (var item in items)
            {
                if (item.ServiceCatalogItemId.HasValue && namesById.TryGetValue(item.ServiceCatalogItemId.Value, out var serviceName))
                {
                    item.ServiceCatalogItemName = serviceName;
                }
            }
        }
    }

    // Exactly one of ProductId/ServiceCatalogItemId, and one price per (PriceList, Product-or-
    // Service) - editing an existing row rather than accumulating duplicates is enforced here
    // since nothing else in this flow would catch it.
    protected override async Task<PriceListItem> MapToEntityAsync(CreateUpdatePriceListItemDto createInput)
    {
        ValidateExactlyOneReference(createInput);

        var alreadyExists = (await Repository.GetListAsync(x =>
                x.PriceListId == createInput.PriceListId &&
                x.ProductId == createInput.ProductId &&
                x.ServiceCatalogItemId == createInput.ServiceCatalogItemId))
            .Any();
        if (alreadyExists)
        {
            throw new UserFriendlyException("This product/service already has a price on this price list.");
        }

        var entity = new PriceListItem(GuidGenerator.Create(), createInput.PriceListId, createInput.UnitPrice);
        MapToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdatePriceListItemDto updateInput, PriceListItem entity)
    {
        ValidateExactlyOneReference(updateInput);
        MapToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void ValidateExactlyOneReference(CreateUpdatePriceListItemDto input)
    {
        if (input.ProductId.HasValue == input.ServiceCatalogItemId.HasValue)
        {
            throw new UserFriendlyException("A price list item must reference exactly one product or service, not both or neither.");
        }
    }

    private static void MapToEntity(CreateUpdatePriceListItemDto input, PriceListItem entity)
    {
        entity.PriceListId = input.PriceListId;
        entity.ProductId = input.ProductId;
        entity.ServiceCatalogItemId = input.ServiceCatalogItemId;
        entity.UnitPrice = input.UnitPrice;
        entity.RateType = input.ServiceCatalogItemId.HasValue ? input.RateType : RateType.Fixed;
    }
}
