using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Sales;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Sales;

[Mapper]
public partial class InvoiceToInvoiceDtoMapper : MapperBase<Invoice, InvoiceDto>
{
    [MapperIgnoreSource(nameof(Invoice.ExtraProperties))]
    [MapperIgnoreSource(nameof(Invoice.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(InvoiceDto.CustomerName))]
    [MapperIgnoreTarget(nameof(InvoiceDto.Subtotal))]
    [MapperIgnoreTarget(nameof(InvoiceDto.TaxAmount))]
    [MapperIgnoreTarget(nameof(InvoiceDto.Total))]
    [MapperIgnoreTarget(nameof(InvoiceDto.AmountPaid))]
    [MapperIgnoreTarget(nameof(InvoiceDto.PaymentStatus))]
    public override partial InvoiceDto Map(Invoice source);

    [MapperIgnoreSource(nameof(Invoice.ExtraProperties))]
    [MapperIgnoreSource(nameof(Invoice.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(InvoiceDto.CustomerName))]
    [MapperIgnoreTarget(nameof(InvoiceDto.Subtotal))]
    [MapperIgnoreTarget(nameof(InvoiceDto.TaxAmount))]
    [MapperIgnoreTarget(nameof(InvoiceDto.Total))]
    [MapperIgnoreTarget(nameof(InvoiceDto.AmountPaid))]
    [MapperIgnoreTarget(nameof(InvoiceDto.PaymentStatus))]
    public override partial void Map(Invoice source, InvoiceDto destination);
}
