using Leitor.Erp.Entities.Cybersecurity;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Cybersecurity;

[Mapper]
public partial class SecurityAssessmentToSecurityAssessmentDtoMapper : MapperBase<SecurityAssessment, SecurityAssessmentDto>
{
    [MapperIgnoreSource(nameof(SecurityAssessment.ExtraProperties))]
    [MapperIgnoreSource(nameof(SecurityAssessment.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(SecurityAssessmentDto.CustomerName))]
    [MapperIgnoreTarget(nameof(SecurityAssessmentDto.ConductedByUserName))]
    public override partial SecurityAssessmentDto Map(SecurityAssessment source);

    [MapperIgnoreSource(nameof(SecurityAssessment.ExtraProperties))]
    [MapperIgnoreSource(nameof(SecurityAssessment.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(SecurityAssessmentDto.CustomerName))]
    [MapperIgnoreTarget(nameof(SecurityAssessmentDto.ConductedByUserName))]
    public override partial void Map(SecurityAssessment source, SecurityAssessmentDto destination);
}
