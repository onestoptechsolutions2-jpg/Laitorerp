using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

public class ExchangeRateAppService :
    CrudAppService<ExchangeRate, ExchangeRateDto, Guid, GetExchangeRateListInput, CreateUpdateExchangeRateDto>
{
    public ExchangeRateAppService(IRepository<ExchangeRate, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Accounting.Default;
        GetListPolicyName = ErpPermissions.Accounting.Default;
        CreatePolicyName = ErpPermissions.Accounting.Edit;
        UpdatePolicyName = ErpPermissions.Accounting.Edit;
        DeletePolicyName = ErpPermissions.Accounting.Edit;
    }

    protected override async Task<IQueryable<ExchangeRate>> CreateFilteredQueryAsync(GetExchangeRateListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(!string.IsNullOrWhiteSpace(input.CurrencyCode), x => x.CurrencyCode == input.CurrencyCode);
    }

    // One rate per (CurrencyCode, RateDate) - the daily sync worker upserts through the repository
    // directly rather than this AppService, so this guard only ever applies to manual entries.
    protected override async Task<ExchangeRate> MapToEntityAsync(CreateUpdateExchangeRateDto createInput)
    {
        var alreadyExists = (await Repository.GetListAsync(
            x => x.CurrencyCode == createInput.CurrencyCode && x.RateDate.Date == createInput.RateDate.Date)).Any();
        if (alreadyExists)
        {
            throw new UserFriendlyException("A rate for this currency on this date already exists.");
        }

        return new ExchangeRate(GuidGenerator.Create(), createInput.CurrencyCode, createInput.RateDate, createInput.RateToBaseCurrency, "Manual");
    }

    protected override async Task MapToEntityAsync(CreateUpdateExchangeRateDto updateInput, ExchangeRate entity)
    {
        var alreadyExists = (await Repository.GetListAsync(
            x => x.CurrencyCode == updateInput.CurrencyCode && x.RateDate.Date == updateInput.RateDate.Date && x.Id != entity.Id)).Any();
        if (alreadyExists)
        {
            throw new UserFriendlyException("A rate for this currency on this date already exists.");
        }

        entity.CurrencyCode = updateInput.CurrencyCode;
        entity.RateDate = updateInput.RateDate;
        entity.RateToBaseCurrency = updateInput.RateToBaseCurrency;
    }
}
