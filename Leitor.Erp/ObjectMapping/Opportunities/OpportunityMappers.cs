using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Services.Dtos.Opportunities;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Opportunities;

[Mapper]
public partial class OpportunityToOpportunityDtoMapper : MapperBase<Opportunity, OpportunityDto>
{
    [MapperIgnoreSource(nameof(Opportunity.ExtraProperties))]
    [MapperIgnoreSource(nameof(Opportunity.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(OpportunityDto.CustomerName))]
    [MapperIgnoreTarget(nameof(OpportunityDto.AssignedToUserName))]
    public override partial OpportunityDto Map(Opportunity source);

    [MapperIgnoreSource(nameof(Opportunity.ExtraProperties))]
    [MapperIgnoreSource(nameof(Opportunity.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(OpportunityDto.CustomerName))]
    [MapperIgnoreTarget(nameof(OpportunityDto.AssignedToUserName))]
    public override partial void Map(Opportunity source, OpportunityDto destination);
}
