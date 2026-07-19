using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class TaxRateToTaxRateDtoMapper : MapperBase<TaxRate, TaxRateDto>
{
    [MapperIgnoreSource(nameof(TaxRate.ExtraProperties))]
    [MapperIgnoreSource(nameof(TaxRate.ConcurrencyStamp))]
    public override partial TaxRateDto Map(TaxRate source);

    [MapperIgnoreSource(nameof(TaxRate.ExtraProperties))]
    [MapperIgnoreSource(nameof(TaxRate.ConcurrencyStamp))]
    public override partial void Map(TaxRate source, TaxRateDto destination);
}
