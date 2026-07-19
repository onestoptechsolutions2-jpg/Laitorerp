using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class SupplierInvoiceToSupplierInvoiceDtoMapper : MapperBase<SupplierInvoice, SupplierInvoiceDto>
{
    [MapperIgnoreSource(nameof(SupplierInvoice.ExtraProperties))]
    [MapperIgnoreSource(nameof(SupplierInvoice.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.VendorName))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.PONumber))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.Total))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.AmountPaid))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.PaymentStatus))]
    public override partial SupplierInvoiceDto Map(SupplierInvoice source);

    [MapperIgnoreSource(nameof(SupplierInvoice.ExtraProperties))]
    [MapperIgnoreSource(nameof(SupplierInvoice.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.VendorName))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.PONumber))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.Total))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.AmountPaid))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceDto.PaymentStatus))]
    public override partial void Map(SupplierInvoice source, SupplierInvoiceDto destination);
}
