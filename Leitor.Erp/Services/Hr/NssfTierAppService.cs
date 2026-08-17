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
public class NssfTierAppService :
    CrudAppService<NssfTier, NssfTierDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateNssfTierDto>
{
    public NssfTierAppService(IRepository<NssfTier, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Payroll.ManageRates;
        GetListPolicyName = ErpPermissions.Payroll.ManageRates;
        CreatePolicyName = ErpPermissions.Payroll.ManageRates;
        UpdatePolicyName = ErpPermissions.Payroll.ManageRates;
        DeletePolicyName = ErpPermissions.Payroll.ManageRates;
    }

    protected override async Task<IQueryable<NssfTier>> CreateFilteredQueryAsync(Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto input)
    {
        input.Sorting ??= $"{nameof(NssfTier.TierNumber)} ASC";
        return await base.CreateFilteredQueryAsync(input);
    }

    protected override Task<NssfTier> MapToEntityAsync(CreateUpdateNssfTierDto createInput)
    {
        var entity = new NssfTier(
            GuidGenerator.Create(), createInput.TierNumber, createInput.LowerBound, createInput.UpperBound,
            createInput.EmployeeRate, createInput.EmployerRate, createInput.EffectiveFrom);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateNssfTierDto updateInput, NssfTier entity)
    {
        entity.TierNumber = updateInput.TierNumber;
        entity.LowerBound = updateInput.LowerBound;
        entity.UpperBound = updateInput.UpperBound;
        entity.EmployeeRate = updateInput.EmployeeRate;
        entity.EmployerRate = updateInput.EmployerRate;
        entity.EffectiveFrom = updateInput.EffectiveFrom;
        return Task.CompletedTask;
    }
}
