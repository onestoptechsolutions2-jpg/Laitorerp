using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class ProductAppService :
    CrudAppService<Product, ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>
{
    public ProductAppService(IRepository<Product, Guid> repository)
        : base(repository)
    {
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
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value);
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
    }
}
