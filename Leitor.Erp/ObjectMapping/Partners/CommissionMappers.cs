using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Services.Dtos.Partners;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Partners;

[Mapper]
public partial class CommissionToCommissionDtoMapper : MapperBase<Commission, CommissionDto>
{
    [MapperIgnoreSource(nameof(Commission.ExtraProperties))]
    [MapperIgnoreSource(nameof(Commission.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CommissionDto.OpportunityName))]
    [MapperIgnoreTarget(nameof(CommissionDto.PartnerName))]
    [MapperIgnoreTarget(nameof(CommissionDto.AgentName))]
    public override partial CommissionDto Map(Commission source);

    [MapperIgnoreSource(nameof(Commission.ExtraProperties))]
    [MapperIgnoreSource(nameof(Commission.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CommissionDto.OpportunityName))]
    [MapperIgnoreTarget(nameof(CommissionDto.PartnerName))]
    [MapperIgnoreTarget(nameof(CommissionDto.AgentName))]
    public override partial void Map(Commission source, CommissionDto destination);
}
