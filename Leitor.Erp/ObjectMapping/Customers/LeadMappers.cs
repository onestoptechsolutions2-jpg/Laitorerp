using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class LeadToLeadDtoMapper : MapperBase<Lead, LeadDto>
{
    [MapperIgnoreSource(nameof(Lead.ExtraProperties))]
    [MapperIgnoreSource(nameof(Lead.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(Lead.NormalizedPhone))]
    [MapperIgnoreTarget(nameof(LeadDto.AssignedToUserName))]
    public override partial LeadDto Map(Lead source);

    [MapperIgnoreSource(nameof(Lead.ExtraProperties))]
    [MapperIgnoreSource(nameof(Lead.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(Lead.NormalizedPhone))]
    [MapperIgnoreTarget(nameof(LeadDto.AssignedToUserName))]
    public override partial void Map(Lead source, LeadDto destination);
}
