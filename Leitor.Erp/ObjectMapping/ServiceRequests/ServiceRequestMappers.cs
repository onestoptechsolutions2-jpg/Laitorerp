using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.ServiceRequests;

[Mapper]
public partial class ServiceRequestToServiceRequestDtoMapper : MapperBase<ServiceRequest, ServiceRequestDto>
{
    [MapperIgnoreSource(nameof(ServiceRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(ServiceRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ServiceRequestDto.CustomerName))]
    [MapperIgnoreTarget(nameof(ServiceRequestDto.ServiceCatalogItemName))]
    public override partial ServiceRequestDto Map(ServiceRequest source);

    [MapperIgnoreSource(nameof(ServiceRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(ServiceRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ServiceRequestDto.CustomerName))]
    [MapperIgnoreTarget(nameof(ServiceRequestDto.ServiceCatalogItemName))]
    public override partial void Map(ServiceRequest source, ServiceRequestDto destination);
}
