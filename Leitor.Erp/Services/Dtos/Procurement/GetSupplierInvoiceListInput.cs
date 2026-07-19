using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GetSupplierInvoiceListInput : PagedAndSortedResultRequestDto
{
    public Guid? VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? Filter { get; set; }
}
