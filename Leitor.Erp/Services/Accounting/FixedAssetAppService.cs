using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

public class FixedAssetAppService :
    CrudAppService<FixedAsset, FixedAssetDto, Guid, GetFixedAssetListInput, CreateUpdateFixedAssetDto>
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<DepreciationEntry, Guid> _depreciationEntryRepository;
    private readonly IDataFilter _dataFilter;

    public FixedAssetAppService(
        IRepository<FixedAsset, Guid> repository,
        IRepository<Account, Guid> accountRepository,
        IRepository<DepreciationEntry, Guid> depreciationEntryRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _accountRepository = accountRepository;
        _depreciationEntryRepository = depreciationEntryRepository;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.FixedAssets.Default;
        GetListPolicyName = ErpPermissions.FixedAssets.Default;
        CreatePolicyName = ErpPermissions.FixedAssets.Create;
        UpdatePolicyName = ErpPermissions.FixedAssets.Edit;
        DeletePolicyName = ErpPermissions.FixedAssets.Delete;
    }

    public override async Task<FixedAssetDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<FixedAssetDto>> GetListAsync(GetFixedAssetListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    protected override async Task<IQueryable<FixedAsset>> CreateFilteredQueryAsync(GetFixedAssetListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(
            !string.IsNullOrWhiteSpace(input.Filter),
            x => x.Name.Contains(input.Filter!) || x.AssetNumber.Contains(input.Filter!)
        );
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<FixedAssetDto> assets)
    {
        var accountIds = assets
            .SelectMany(x => new[] { x.AssetAccountId, x.DepreciationExpenseAccountId, x.AccumulatedDepreciationAccountId })
            .Distinct()
            .ToList();
        var namesById = accountIds.Count > 0
            ? (await _accountRepository.GetListAsync(x => accountIds.Contains(x.Id))).ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}")
            : new Dictionary<Guid, string>();

        var assetIds = assets.Select(x => x.Id).ToList();
        var depreciationByAssetId = assetIds.Count > 0
            ? (await _depreciationEntryRepository.GetListAsync(x => assetIds.Contains(x.FixedAssetId))).ToLookup(x => x.FixedAssetId)
            : Enumerable.Empty<DepreciationEntry>().ToLookup(x => x.FixedAssetId);

        foreach (var asset in assets)
        {
            asset.AssetAccountName = namesById.GetValueOrDefault(asset.AssetAccountId);
            asset.DepreciationExpenseAccountName = namesById.GetValueOrDefault(asset.DepreciationExpenseAccountId);
            asset.AccumulatedDepreciationAccountName = namesById.GetValueOrDefault(asset.AccumulatedDepreciationAccountId);

            asset.AccumulatedDepreciation = depreciationByAssetId[asset.Id].Sum(x => x.Amount);
            asset.BookValue = asset.PurchaseCost - asset.AccumulatedDepreciation;
            asset.IsFullyDepreciated = asset.AccumulatedDepreciation >= asset.PurchaseCost - asset.SalvageValue;
        }
    }

    protected override async Task<FixedAsset> MapToEntityAsync(CreateUpdateFixedAssetDto createInput)
    {
        var assetNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "FA-");
        var entity = new FixedAsset(GuidGenerator.Create(), assetNumber, createInput.Name);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateFixedAssetDto updateInput, FixedAsset entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateFixedAssetDto input, FixedAsset entity)
    {
        entity.Name = input.Name;
        entity.Category = input.Category;
        entity.PurchaseDate = input.PurchaseDate;
        entity.PurchaseCost = input.PurchaseCost;
        entity.SalvageValue = input.SalvageValue;
        entity.UsefulLifeMonths = input.UsefulLifeMonths;
        entity.Status = input.Status;
        entity.AssetAccountId = input.AssetAccountId;
        entity.DepreciationExpenseAccountId = input.DepreciationExpenseAccountId;
        entity.AccumulatedDepreciationAccountId = input.AccumulatedDepreciationAccountId;
    }
}
