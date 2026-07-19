using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GetSupplierInvoiceLineListInput : PagedAndSortedResultRequestDto
{
    public Guid? SupplierInvoiceId { get; set; }
}
