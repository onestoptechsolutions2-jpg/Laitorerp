using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetQuoteLineListInput : PagedAndSortedResultRequestDto
{
    public Guid? QuoteId { get; set; }
}
