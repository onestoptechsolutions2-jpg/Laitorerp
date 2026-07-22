using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Accounting;

[Mapper]
public partial class BankAccountToBankAccountDtoMapper : MapperBase<BankAccount, BankAccountDto>
{
    [MapperIgnoreSource(nameof(BankAccount.ExtraProperties))]
    [MapperIgnoreSource(nameof(BankAccount.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(BankAccountDto.LinkedGlAccountName))]
    [MapperIgnoreTarget(nameof(BankAccountDto.GlBalance))]
    [MapperIgnoreTarget(nameof(BankAccountDto.UnreconciledStatementLineCount))]
    public override partial BankAccountDto Map(BankAccount source);

    [MapperIgnoreSource(nameof(BankAccount.ExtraProperties))]
    [MapperIgnoreSource(nameof(BankAccount.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(BankAccountDto.LinkedGlAccountName))]
    [MapperIgnoreTarget(nameof(BankAccountDto.GlBalance))]
    [MapperIgnoreTarget(nameof(BankAccountDto.UnreconciledStatementLineCount))]
    public override partial void Map(BankAccount source, BankAccountDto destination);
}

[Mapper]
public partial class BankStatementLineToBankStatementLineDtoMapper : MapperBase<BankStatementLine, BankStatementLineDto>
{
    [MapperIgnoreSource(nameof(BankStatementLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(BankStatementLine.ConcurrencyStamp))]
    public override partial BankStatementLineDto Map(BankStatementLine source);

    [MapperIgnoreSource(nameof(BankStatementLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(BankStatementLine.ConcurrencyStamp))]
    public override partial void Map(BankStatementLine source, BankStatementLineDto destination);
}
