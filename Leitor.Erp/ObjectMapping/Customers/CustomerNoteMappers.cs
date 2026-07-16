using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class CustomerNoteToCustomerNoteDtoMapper : MapperBase<CustomerNote, CustomerNoteDto>
{
    [MapperIgnoreSource(nameof(CustomerNote.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerNote.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CustomerNoteDto.CreatorUserName))]
    public override partial CustomerNoteDto Map(CustomerNote source);

    [MapperIgnoreSource(nameof(CustomerNote.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerNote.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CustomerNoteDto.CreatorUserName))]
    public override partial void Map(CustomerNote source, CustomerNoteDto destination);
}
