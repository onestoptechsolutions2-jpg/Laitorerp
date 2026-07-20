using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Accounting;

[Mapper]
public partial class CurrencyToCurrencyDtoMapper : MapperBase<Currency, CurrencyDto>
{
    [MapperIgnoreSource(nameof(Currency.ExtraProperties))]
    [MapperIgnoreSource(nameof(Currency.ConcurrencyStamp))]
    public override partial CurrencyDto Map(Currency source);

    [MapperIgnoreSource(nameof(Currency.ExtraProperties))]
    [MapperIgnoreSource(nameof(Currency.ConcurrencyStamp))]
    public override partial void Map(Currency source, CurrencyDto destination);
}
