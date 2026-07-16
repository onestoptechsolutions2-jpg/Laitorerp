using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class InvoiceLineToInvoiceLineDtoMapper : MapperBase<InvoiceLine, InvoiceLineDto>
{
    [MapperIgnoreSource(nameof(InvoiceLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(InvoiceLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(InvoiceLineDto.LineTotal))]
    public override partial InvoiceLineDto Map(InvoiceLine source);

    [MapperIgnoreSource(nameof(InvoiceLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(InvoiceLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(InvoiceLineDto.LineTotal))]
    public override partial void Map(InvoiceLine source, InvoiceLineDto destination);
}
