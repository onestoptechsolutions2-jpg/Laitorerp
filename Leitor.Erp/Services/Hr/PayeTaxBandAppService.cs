using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Hr;

[RequiresFeature(ErpFeatures.HumanResources)]
public class PayeTaxBandAppService :
    CrudAppService<PayeTaxBand, PayeTaxBandDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdatePayeTaxBandDto>
{
    public PayeTaxBandAppService(IRepository<PayeTaxBand, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Payroll.ManageRates;
        GetListPolicyName = ErpPermissions.Payroll.ManageRates;
        CreatePolicyName = ErpPermissions.Payroll.ManageRates;
        UpdatePolicyName = ErpPermissions.Payroll.ManageRates;
        DeletePolicyName = ErpPermissions.Payroll.ManageRates;
    }

    protected override async Task<IQueryable<PayeTaxBand>> CreateFilteredQueryAsync(Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto input)
    {
        input.Sorting ??= $"{nameof(PayeTaxBand.LowerBound)} ASC";
        return await base.CreateFilteredQueryAsync(input);
    }

    protected override Task<PayeTaxBand> MapToEntityAsync(CreateUpdatePayeTaxBandDto createInput)
    {
        var entity = new PayeTaxBand(GuidGenerator.Create(), createInput.LowerBound, createInput.UpperBound, createInput.Rate, createInput.EffectiveFrom);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdatePayeTaxBandDto updateInput, PayeTaxBand entity)
    {
        entity.LowerBound = updateInput.LowerBound;
        entity.UpperBound = updateInput.UpperBound;
        entity.Rate = updateInput.Rate;
        entity.EffectiveFrom = updateInput.EffectiveFrom;
        return Task.CompletedTask;
    }
}
