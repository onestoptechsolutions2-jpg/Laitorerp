using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Dtos.Support;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Support;

[Mapper]
public partial class ProblemToProblemDtoMapper : MapperBase<Problem, ProblemDto>
{
    [MapperIgnoreSource(nameof(Problem.ExtraProperties))]
    [MapperIgnoreSource(nameof(Problem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProblemDto.LinkedTicketCount))]
    public override partial ProblemDto Map(Problem source);

    [MapperIgnoreSource(nameof(Problem.ExtraProperties))]
    [MapperIgnoreSource(nameof(Problem.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ProblemDto.LinkedTicketCount))]
    public override partial void Map(Problem source, ProblemDto destination);
}
