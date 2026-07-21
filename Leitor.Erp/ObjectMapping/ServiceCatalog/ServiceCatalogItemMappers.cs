using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.ServiceCatalog;

[Mapper]
public partial class ServiceCatalogItemToServiceCatalogItemDtoMapper : MapperBase<ServiceCatalogItem, ServiceCatalogItemDto>
{
    [MapperIgnoreSource(nameof(ServiceCatalogItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(ServiceCatalogItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ServiceCatalogItemDto.OwnerUserName))]
    public override partial ServiceCatalogItemDto Map(ServiceCatalogItem source);

    [MapperIgnoreSource(nameof(ServiceCatalogItem.ExtraProperties))]
    [MapperIgnoreSource(nameof(ServiceCatalogItem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ServiceCatalogItemDto.OwnerUserName))]
    public override partial void Map(ServiceCatalogItem source, ServiceCatalogItemDto destination);
}
