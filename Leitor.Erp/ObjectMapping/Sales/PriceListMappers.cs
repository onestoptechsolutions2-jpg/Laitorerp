using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class PriceListToPriceListDtoMapper : MapperBase<PriceList, PriceListDto>
{
    [MapperIgnoreSource(nameof(PriceList.ExtraProperties))]
    [MapperIgnoreSource(nameof(PriceList.ConcurrencyStamp))]
    public override partial PriceListDto Map(PriceList source);

    [MapperIgnoreSource(nameof(PriceList.ExtraProperties))]
    [MapperIgnoreSource(nameof(PriceList.ConcurrencyStamp))]
    public override partial void Map(PriceList source, PriceListDto destination);
}
