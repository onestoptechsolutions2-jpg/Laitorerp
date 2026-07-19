using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Procurement;

[Mapper]
public partial class SupplierInvoiceLineToSupplierInvoiceLineDtoMapper : MapperBase<SupplierInvoiceLine, SupplierInvoiceLineDto>
{
    [MapperIgnoreSource(nameof(SupplierInvoiceLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(SupplierInvoiceLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceLineDto.LineTotal))]
    public override partial SupplierInvoiceLineDto Map(SupplierInvoiceLine source);

    [MapperIgnoreSource(nameof(SupplierInvoiceLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(SupplierInvoiceLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(SupplierInvoiceLineDto.LineTotal))]
    public override partial void Map(SupplierInvoiceLine source, SupplierInvoiceLineDto destination);
}
