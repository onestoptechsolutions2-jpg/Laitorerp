using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Accounting;

[Mapper]
public partial class FixedAssetToFixedAssetDtoMapper : MapperBase<FixedAsset, FixedAssetDto>
{
    [MapperIgnoreSource(nameof(FixedAsset.ExtraProperties))]
    [MapperIgnoreSource(nameof(FixedAsset.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.AssetAccountName))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.DepreciationExpenseAccountName))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.AccumulatedDepreciationAccountName))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.AccumulatedDepreciation))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.BookValue))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.IsFullyDepreciated))]
    public override partial FixedAssetDto Map(FixedAsset source);

    [MapperIgnoreSource(nameof(FixedAsset.ExtraProperties))]
    [MapperIgnoreSource(nameof(FixedAsset.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.AssetAccountName))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.DepreciationExpenseAccountName))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.AccumulatedDepreciationAccountName))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.AccumulatedDepreciation))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.BookValue))]
    [MapperIgnoreTarget(nameof(FixedAssetDto.IsFullyDepreciated))]
    public override partial void Map(FixedAsset source, FixedAssetDto destination);
}

[Mapper]
public partial class DepreciationEntryToDepreciationEntryDtoMapper : MapperBase<DepreciationEntry, DepreciationEntryDto>
{
    [MapperIgnoreSource(nameof(DepreciationEntry.ExtraProperties))]
    [MapperIgnoreSource(nameof(DepreciationEntry.ConcurrencyStamp))]
    public override partial DepreciationEntryDto Map(DepreciationEntry source);

    [MapperIgnoreSource(nameof(DepreciationEntry.ExtraProperties))]
    [MapperIgnoreSource(nameof(DepreciationEntry.ConcurrencyStamp))]
    public override partial void Map(DepreciationEntry source, DepreciationEntryDto destination);
}
