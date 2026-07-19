using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class PriceListAppService :
    CrudAppService<PriceList, PriceListDto, Guid, PagedAndSortedResultRequestDto, CreateUpdatePriceListDto>
{
    public PriceListAppService(IRepository<PriceList, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Edit;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Edit;
    }

    // CreateUpdatePriceListDto -> PriceList is mapped manually rather than via Mapperly - same
    // reason as every other entity in this app (protected Id setter).
    protected override Task<PriceList> MapToEntityAsync(CreateUpdatePriceListDto createInput)
    {
        var entity = new PriceList(GuidGenerator.Create(), createInput.Name);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdatePriceListDto updateInput, PriceList entity)
    {
        entity.Name = updateInput.Name;
        return Task.CompletedTask;
    }
}
