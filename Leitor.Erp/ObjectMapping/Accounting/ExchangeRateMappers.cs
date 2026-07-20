using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Accounting;

[Mapper]
public partial class ExchangeRateToExchangeRateDtoMapper : MapperBase<ExchangeRate, ExchangeRateDto>
{
    [MapperIgnoreSource(nameof(ExchangeRate.ExtraProperties))]
    [MapperIgnoreSource(nameof(ExchangeRate.ConcurrencyStamp))]
    public override partial ExchangeRateDto Map(ExchangeRate source);

    [MapperIgnoreSource(nameof(ExchangeRate.ExtraProperties))]
    [MapperIgnoreSource(nameof(ExchangeRate.ConcurrencyStamp))]
    public override partial void Map(ExchangeRate source, ExchangeRateDto destination);
}
