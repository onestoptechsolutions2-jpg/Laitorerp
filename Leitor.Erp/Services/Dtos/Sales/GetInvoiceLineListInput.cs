using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetInvoiceLineListInput : PagedAndSortedResultRequestDto
{
    public Guid? InvoiceId { get; set; }
}
