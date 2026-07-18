using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class ProductVendorAppService :
    CrudAppService<ProductVendor, ProductVendorDto, Guid, GetProductVendorListInput, CreateUpdateProductVendorDto>
{
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public ProductVendorAppService(
        IRepository<ProductVendor, Guid> repository,
        IRepository<Vendor, Guid> vendorRepository)
        : base(repository)
    {
        _vendorRepository = vendorRepository;

        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Edit;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Edit;
    }

    protected override async Task<IQueryable<ProductVendor>> CreateFilteredQueryAsync(GetProductVendorListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.ProductId.HasValue, x => x.ProductId == input.ProductId!.Value);
    }

    public override async Task<ProductVendorDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveVendorNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ProductVendorDto>> GetListAsync(GetProductVendorListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveVendorNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveVendorNamesAsync(IReadOnlyCollection<ProductVendorDto> productVendors)
    {
        var vendorIds = productVendors.Select(x => x.VendorId).Distinct().ToList();
        var vendors = await _vendorRepository.GetListAsync(x => vendorIds.Contains(x.Id));
        var namesById = vendors.ToDictionary(x => x.Id, x => x.Name);

        foreach (var productVendor in productVendors)
        {
            if (namesById.TryGetValue(productVendor.VendorId, out var vendorName))
            {
                productVendor.VendorName = vendorName;
            }
        }
    }

    // CreateUpdateProductVendorDto -> ProductVendor is mapped manually rather than via Mapperly -
    // same reason as every other entity in this app (protected Id setter).
    protected override async Task<ProductVendor> MapToEntityAsync(CreateUpdateProductVendorDto createInput)
    {
        if (createInput.IsPreferred)
        {
            await ClearOtherPreferredVendorsAsync(createInput.ProductId, currentId: null);
        }

        var entity = new ProductVendor(GuidGenerator.Create(), createInput.ProductId, createInput.VendorId, createInput.Cost);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateProductVendorDto updateInput, ProductVendor entity)
    {
        if (updateInput.IsPreferred)
        {
            await ClearOtherPreferredVendorsAsync(updateInput.ProductId, currentId: entity.Id);
        }

        CopyToEntity(updateInput, entity);
    }

    // Keeps "the preferred vendor" unambiguous per product, since the Create-Purchase-Order-from-
    // Order flow pre-fills cost from whichever ProductVendor row has IsPreferred set.
    private async Task ClearOtherPreferredVendorsAsync(Guid productId, Guid? currentId)
    {
        var others = await Repository.GetListAsync(x =>
            x.ProductId == productId && x.IsPreferred && x.Id != (currentId ?? Guid.Empty));

        foreach (var other in others)
        {
            other.IsPreferred = false;
        }

        if (others.Count > 0)
        {
            await Repository.UpdateManyAsync(others);
        }
    }

    private static void CopyToEntity(CreateUpdateProductVendorDto input, ProductVendor entity)
    {
        entity.ProductId = input.ProductId;
        entity.VendorId = input.VendorId;
        entity.VendorSku = input.VendorSku;
        entity.Cost = input.Cost;
        entity.LeadTimeDays = input.LeadTimeDays;
        entity.IsPreferred = input.IsPreferred;
        entity.Notes = input.Notes;
    }
}
