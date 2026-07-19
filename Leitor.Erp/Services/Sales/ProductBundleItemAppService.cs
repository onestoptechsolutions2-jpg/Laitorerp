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

public class ProductBundleItemAppService :
    CrudAppService<ProductBundleItem, ProductBundleItemDto, Guid, GetProductBundleItemListInput, CreateUpdateProductBundleItemDto>
{
    private readonly IRepository<Product, Guid> _productRepository;

    public ProductBundleItemAppService(
        IRepository<ProductBundleItem, Guid> repository,
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

    protected override async Task<IQueryable<ProductBundleItem>> CreateFilteredQueryAsync(GetProductBundleItemListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.BundleProductId.HasValue, x => x.BundleProductId == input.BundleProductId!.Value);
    }

    public override async Task<ProductBundleItemDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveProductNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ProductBundleItemDto>> GetListAsync(GetProductBundleItemListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveProductNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveProductNamesAsync(IReadOnlyCollection<ProductBundleItemDto> items)
    {
        var productIds = items.Select(x => x.ComponentProductId).Distinct().ToList();
        var products = await _productRepository.GetListAsync(x => productIds.Contains(x.Id));
        var namesById = products.ToDictionary(x => x.Id, x => x.Name);

        foreach (var item in items)
        {
            if (namesById.TryGetValue(item.ComponentProductId, out var name))
            {
                item.ComponentProductName = name;
            }
        }
    }

    // A bundle component can't itself be a bundle (no nested explosion logic exists downstream in
    // QuoteLineAppService/OrderLineAppService) and can't reference itself.
    protected override async Task<ProductBundleItem> MapToEntityAsync(CreateUpdateProductBundleItemDto createInput)
    {
        await ValidateAsync(createInput);
        return new ProductBundleItem(GuidGenerator.Create(), createInput.BundleProductId, createInput.ComponentProductId)
        {
            Quantity = createInput.Quantity
        };
    }

    protected override async Task MapToEntityAsync(CreateUpdateProductBundleItemDto updateInput, ProductBundleItem entity)
    {
        await ValidateAsync(updateInput);
        entity.BundleProductId = updateInput.BundleProductId;
        entity.ComponentProductId = updateInput.ComponentProductId;
        entity.Quantity = updateInput.Quantity;
    }

    private async Task ValidateAsync(CreateUpdateProductBundleItemDto input)
    {
        if (input.ComponentProductId == input.BundleProductId)
        {
            throw new UserFriendlyException("A bundle cannot contain itself as a component.");
        }

        var component = await _productRepository.GetAsync(input.ComponentProductId);
        if (component.IsBundle)
        {
            throw new UserFriendlyException("A bundle's components must be regular products, not other bundles.");
        }
    }
}
