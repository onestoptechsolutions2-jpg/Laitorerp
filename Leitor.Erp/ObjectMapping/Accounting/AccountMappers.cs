using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Accounting;

[Mapper]
public partial class AccountToAccountDtoMapper : MapperBase<Account, AccountDto>
{
    [MapperIgnoreSource(nameof(Account.ExtraProperties))]
    [MapperIgnoreSource(nameof(Account.ConcurrencyStamp))]
    public override partial AccountDto Map(Account source);

    [MapperIgnoreSource(nameof(Account.ExtraProperties))]
    [MapperIgnoreSource(nameof(Account.ConcurrencyStamp))]
    public override partial void Map(Account source, AccountDto destination);
}
