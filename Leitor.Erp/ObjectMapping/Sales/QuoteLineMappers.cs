using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class QuoteLineToQuoteLineDtoMapper : MapperBase<QuoteLine, QuoteLineDto>
{
    [MapperIgnoreSource(nameof(QuoteLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(QuoteLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(QuoteLineDto.LineTotal))]
    [MapperIgnoreTarget(nameof(QuoteLineDto.MarginPercent))]
    public override partial QuoteLineDto Map(QuoteLine source);

    [MapperIgnoreSource(nameof(QuoteLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(QuoteLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(QuoteLineDto.LineTotal))]
    [MapperIgnoreTarget(nameof(QuoteLineDto.MarginPercent))]
    public override partial void Map(QuoteLine source, QuoteLineDto destination);
}
