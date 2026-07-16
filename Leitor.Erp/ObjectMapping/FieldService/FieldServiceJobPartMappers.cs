using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Services.Dtos.FieldService;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.FieldService;

[Mapper]
public partial class FieldServiceJobPartToFieldServiceJobPartDtoMapper : MapperBase<FieldServiceJobPart, FieldServiceJobPartDto>
{
    [MapperIgnoreSource(nameof(FieldServiceJobPart.ExtraProperties))]
    [MapperIgnoreSource(nameof(FieldServiceJobPart.ConcurrencyStamp))]
    public override partial FieldServiceJobPartDto Map(FieldServiceJobPart source);

    [MapperIgnoreSource(nameof(FieldServiceJobPart.ExtraProperties))]
    [MapperIgnoreSource(nameof(FieldServiceJobPart.ConcurrencyStamp))]
    public override partial void Map(FieldServiceJobPart source, FieldServiceJobPartDto destination);
}
