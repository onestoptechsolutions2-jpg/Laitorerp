using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class QuoteToQuoteDtoMapper : MapperBase<Quote, QuoteDto>
{
    [MapperIgnoreSource(nameof(Quote.ExtraProperties))]
    [MapperIgnoreSource(nameof(Quote.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(QuoteDto.CustomerName))]
    [MapperIgnoreTarget(nameof(QuoteDto.Total))]
    public override partial QuoteDto Map(Quote source);

    [MapperIgnoreSource(nameof(Quote.ExtraProperties))]
    [MapperIgnoreSource(nameof(Quote.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(QuoteDto.CustomerName))]
    [MapperIgnoreTarget(nameof(QuoteDto.Total))]
    public override partial void Map(Quote source, QuoteDto destination);
}
