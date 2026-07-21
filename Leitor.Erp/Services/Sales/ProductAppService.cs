using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class ProductAppService :
    CrudAppService<Product, ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<StockMovement, Guid> stockMovementRepository)
        : base(repository)
    {
        _categoryRepository = categoryRepository;
        _stockMovementRepository = stockMovementRepository;

        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Create;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Delete;
    }

    protected override async Task<IQueryable<Product>> CreateFilteredQueryAsync(GetProductListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value)
            .WhereIf(input.CategoryId.HasValue, x => x.CategoryId == input.CategoryId!.Value);
    }

    public override async Task<ProductDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ProductDto> products)
    {
        var categoryIds = products.Where(x => x.CategoryId.HasValue).Select(x => x.CategoryId!.Value).Distinct().ToList();
        var namesById = categoryIds.Count > 0
            ? (await _categoryRepository.GetListAsync(x => categoryIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var trackedProductIds = products.Where(x => x.TrackInventory).Select(x => x.Id).ToList();
        var onHandByProductId = trackedProductIds.Count > 0
            ? (await _stockMovementRepository.GetListAsync(x => trackedProductIds.Contains(x.ProductId)))
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity))
            : new Dictionary<Guid, decimal>();

        foreach (var product in products)
        {
            if (product.CategoryId.HasValue && namesById.TryGetValue(product.CategoryId.Value, out var categoryName))
            {
                product.CategoryName = categoryName;
            }

            if (product.TrackInventory)
            {
                product.QuantityOnHand = onHandByProductId.GetValueOrDefault(product.Id);
            }
        }
    }

    // CreateUpdateProductDto -> Product is mapped manually rather than via Mapperly - same reason
    // as every other entity in this app: Product's Id has a protected setter and its constructor
    // needs a generated Guid the DTO has no source for.
    protected override Task<Product> MapToEntityAsync(CreateUpdateProductDto createInput)
    {
        var entity = new Product(GuidGenerator.Create(), createInput.Name, createInput.UnitPrice);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateProductDto updateInput, Product entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateProductDto input, Product entity)
    {
        entity.Name = input.Name;
        entity.Sku = input.Sku;
        entity.Description = input.Description;
        entity.Type = input.Type;
        entity.UnitPrice = input.UnitPrice;
        entity.IsActive = input.IsActive;
        entity.Cost = input.Cost;
        entity.TaxRateId = input.TaxRateId;
        entity.CategoryId = input.CategoryId;
        entity.IsBundle = input.IsBundle;
        entity.TrackInventory = input.TrackInventory;
        entity.ReorderPoint = input.ReorderPoint;
        entity.ReorderQuantity = input.ReorderQuantity;
    }
}
