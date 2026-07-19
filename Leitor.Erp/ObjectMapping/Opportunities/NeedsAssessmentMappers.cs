using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Services.Dtos.Opportunities;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Opportunities;

[Mapper]
public partial class NeedsAssessmentToNeedsAssessmentDtoMapper : MapperBase<NeedsAssessment, NeedsAssessmentDto>
{
    [MapperIgnoreSource(nameof(NeedsAssessment.ExtraProperties))]
    [MapperIgnoreSource(nameof(NeedsAssessment.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(NeedsAssessmentDto.ConductedByUserName))]
    public override partial NeedsAssessmentDto Map(NeedsAssessment source);

    [MapperIgnoreSource(nameof(NeedsAssessment.ExtraProperties))]
    [MapperIgnoreSource(nameof(NeedsAssessment.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(NeedsAssessmentDto.ConductedByUserName))]
    public override partial void Map(NeedsAssessment source, NeedsAssessmentDto destination);
}
