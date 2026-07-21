using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Services.Dtos.FieldService;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.FieldService;

[Mapper]
public partial class FieldServiceJobToFieldServiceJobDtoMapper : MapperBase<FieldServiceJob, FieldServiceJobDto>
{
    [MapperIgnoreSource(nameof(FieldServiceJob.ExtraProperties))]
    [MapperIgnoreSource(nameof(FieldServiceJob.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.CustomerName))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.AssignedToUserName))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.VendorName))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.ConfigurationItemName))]
    public override partial FieldServiceJobDto Map(FieldServiceJob source);

    [MapperIgnoreSource(nameof(FieldServiceJob.ExtraProperties))]
    [MapperIgnoreSource(nameof(FieldServiceJob.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.CustomerName))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.AssignedToUserName))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.VendorName))]
    [MapperIgnoreTarget(nameof(FieldServiceJobDto.ConfigurationItemName))]
    public override partial void Map(FieldServiceJob source, FieldServiceJobDto destination);
}
