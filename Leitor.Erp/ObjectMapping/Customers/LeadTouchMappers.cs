using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class LeadTouchToLeadTouchDtoMapper : MapperBase<LeadTouch, LeadTouchDto>
{
    [MapperIgnoreSource(nameof(LeadTouch.ExtraProperties))]
    [MapperIgnoreSource(nameof(LeadTouch.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(LeadTouchDto.CreatorUserName))]
    public override partial LeadTouchDto Map(LeadTouch source);

    [MapperIgnoreSource(nameof(LeadTouch.ExtraProperties))]
    [MapperIgnoreSource(nameof(LeadTouch.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(LeadTouchDto.CreatorUserName))]
    public override partial void Map(LeadTouch source, LeadTouchDto destination);
}
