using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Services.Dtos.Opportunities;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Opportunities;

[Mapper]
public partial class ProposalToProposalDtoMapper : MapperBase<Proposal, ProposalDto>
{
    [MapperIgnoreSource(nameof(Proposal.ExtraProperties))]
    [MapperIgnoreSource(nameof(Proposal.ConcurrencyStamp))]
    public override partial ProposalDto Map(Proposal source);

    [MapperIgnoreSource(nameof(Proposal.ExtraProperties))]
    [MapperIgnoreSource(nameof(Proposal.ConcurrencyStamp))]
    public override partial void Map(Proposal source, ProposalDto destination);
}
