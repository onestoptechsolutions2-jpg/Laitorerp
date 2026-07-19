using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
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

    public PriceListItemAppService(
        IRepository<PriceListItem, Guid> repository,
        IRepository<Product, Guid> productRepository)
        : base(repository)
    {
        _productRepository = productRepository;

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
        await ResolveProductNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<PriceListItemDto>> GetListAsync(GetPriceListItemListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveProductNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveProductNamesAsync(IReadOnlyCollection<PriceListItemDto> items)
    {
        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _productRepository.GetListAsync(x => productIds.Contains(x.Id));
        var namesById = products.ToDictionary(x => x.Id, x => x.Name);

        foreach (var item in items)
        {
            if (namesById.TryGetValue(item.ProductId, out var productName))
            {
                item.ProductName = productName;
            }
        }
    }

    // One price per Product per PriceList - editing an existing row rather than accumulating
    // duplicates is enforced here since nothing else in this flow would catch it.
    protected override async Task<PriceListItem> MapToEntityAsync(CreateUpdatePriceListItemDto createInput)
    {
        var alreadyExists = (await Repository.GetListAsync(
            x => x.PriceListId == createInput.PriceListId && x.ProductId == createInput.ProductId)).Any();
        if (alreadyExists)
        {
            throw new UserFriendlyException("This product already has a price on this price list.");
        }

        var entity = new PriceListItem(GuidGenerator.Create(), createInput.PriceListId, createInput.ProductId, createInput.UnitPrice);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdatePriceListItemDto updateInput, PriceListItem entity)
    {
        entity.PriceListId = updateInput.PriceListId;
        entity.ProductId = updateInput.ProductId;
        entity.UnitPrice = updateInput.UnitPrice;
        return Task.CompletedTask;
    }
}
