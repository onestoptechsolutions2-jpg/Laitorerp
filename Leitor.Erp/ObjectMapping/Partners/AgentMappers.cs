using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Services.Dtos.Partners;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Partners;

[Mapper]
public partial class AgentToAgentDtoMapper : MapperBase<Agent, AgentDto>
{
    [MapperIgnoreSource(nameof(Agent.ExtraProperties))]
    [MapperIgnoreSource(nameof(Agent.ConcurrencyStamp))]
    public override partial AgentDto Map(Agent source);

    [MapperIgnoreSource(nameof(Agent.ExtraProperties))]
    [MapperIgnoreSource(nameof(Agent.ConcurrencyStamp))]
    public override partial void Map(Agent source, AgentDto destination);
}
