using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class TaxRateAppService :
    CrudAppService<TaxRate, TaxRateDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateTaxRateDto>
{
    public TaxRateAppService(IRepository<TaxRate, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Edit;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Edit;
    }

    // CreateUpdateTaxRateDto -> TaxRate is mapped manually rather than via Mapperly - same reason
    // as every other entity in this app (protected Id setter).
    protected override async Task<TaxRate> MapToEntityAsync(CreateUpdateTaxRateDto createInput)
    {
        if (createInput.IsDefault)
        {
            await ClearOtherDefaultsAsync(currentId: null);
        }

        var entity = new TaxRate(GuidGenerator.Create(), createInput.Name, createInput.Percent);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateTaxRateDto updateInput, TaxRate entity)
    {
        if (updateInput.IsDefault)
        {
            await ClearOtherDefaultsAsync(currentId: entity.Id);
        }

        CopyToEntity(updateInput, entity);
    }

    // Keeps "the default tax rate" unambiguous, since it's what every line without its own rate
    // falls back to - same pattern as ProductVendorAppService.ClearOtherPreferredVendorsAsync.
    private async Task ClearOtherDefaultsAsync(Guid? currentId)
    {
        var others = await Repository.GetListAsync(x => x.IsDefault && x.Id != (currentId ?? Guid.Empty));

        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        if (others.Count > 0)
        {
            await Repository.UpdateManyAsync(others);
        }
    }

    private static void CopyToEntity(CreateUpdateTaxRateDto input, TaxRate entity)
    {
        entity.Name = input.Name;
        entity.Percent = input.Percent;
        entity.IsDefault = input.IsDefault;
    }
}
