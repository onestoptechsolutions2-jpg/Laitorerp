using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class GetQuoteListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public string? Filter { get; set; }
}
