using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Assets;

[Mapper]
public partial class ConfigurationItemToConfigurationItemDtoMapper : MapperBase<ConfigurationItem, ConfigurationItemDto>
{
    [MapperIgnoreSource(nameof(ConfigurationItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(ConfigurationItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ConfigurationItemDto.CustomerName))]
    public override partial ConfigurationItemDto Map(ConfigurationItem source);

    [MapperIgnoreSource(nameof(ConfigurationItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(ConfigurationItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ConfigurationItemDto.CustomerName))]
    public override partial void Map(ConfigurationItem source, ConfigurationItemDto destination);
}
