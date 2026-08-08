using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Assets;

[Mapper]
public partial class AssetCredentialToAssetCredentialDtoMapper : MapperBase<AssetCredential, AssetCredentialDto>
{
    // EncryptedValue is deliberately never mapped onto the DTO - see AssetCredential's own comment.
    [MapperIgnoreSource(nameof(AssetCredential.EncryptedValue))]
    [MapperIgnoreSource(nameof(AssetCredential.ExtraProperties))]
    [MapperIgnoreSource(nameof(AssetCredential.ConcurrencyStamp))]
    public override partial AssetCredentialDto Map(AssetCredential source);

    [MapperIgnoreSource(nameof(AssetCredential.EncryptedValue))]
    [MapperIgnoreSource(nameof(AssetCredential.ExtraProperties))]
    [MapperIgnoreSource(nameof(AssetCredential.ConcurrencyStamp))]
    public override partial void Map(AssetCredential source, AssetCredentialDto destination);
}
