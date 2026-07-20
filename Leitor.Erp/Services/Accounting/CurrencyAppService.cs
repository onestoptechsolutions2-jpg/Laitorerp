using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

public class CurrencyAppService :
    CrudAppService<Currency, CurrencyDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateCurrencyDto>
{
    public CurrencyAppService(IRepository<Currency, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Accounting.Default;
        GetListPolicyName = ErpPermissions.Accounting.Default;
        CreatePolicyName = ErpPermissions.Accounting.Edit;
        UpdatePolicyName = ErpPermissions.Accounting.Edit;
        DeletePolicyName = ErpPermissions.Accounting.Edit;
    }

    protected override async Task<Currency> MapToEntityAsync(CreateUpdateCurrencyDto createInput)
    {
        if (createInput.IsBaseCurrency)
        {
            await ClearOtherBaseCurrenciesAsync(currentId: null);
        }

        var entity = new Currency(GuidGenerator.Create(), createInput.Code, createInput.Name, createInput.Symbol);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateCurrencyDto updateInput, Currency entity)
    {
        if (updateInput.IsBaseCurrency)
        {
            await ClearOtherBaseCurrenciesAsync(currentId: entity.Id);
        }

        CopyToEntity(updateInput, entity);
    }

    // Keeps "the base currency" unambiguous - every other currency's rate is expressed relative
    // to this one. Same pattern as TaxRateAppService.ClearOtherDefaultsAsync.
    private async Task ClearOtherBaseCurrenciesAsync(Guid? currentId)
    {
        var others = await Repository.GetListAsync(x => x.IsBaseCurrency && x.Id != (currentId ?? Guid.Empty));

        foreach (var other in others)
        {
            other.IsBaseCurrency = false;
        }

        if (others.Count > 0)
        {
            await Repository.UpdateManyAsync(others);
        }
    }

    private static void CopyToEntity(CreateUpdateCurrencyDto input, Currency entity)
    {
        entity.Code = input.Code;
        entity.Name = input.Name;
        entity.Symbol = input.Symbol;
        entity.IsBaseCurrency = input.IsBaseCurrency;
        entity.IsActive = input.IsActive;
    }
}
