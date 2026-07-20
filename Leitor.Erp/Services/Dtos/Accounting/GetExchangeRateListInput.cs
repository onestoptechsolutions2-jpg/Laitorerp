using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class GetExchangeRateListInput : PagedAndSortedResultRequestDto
{
    public string? CurrencyCode { get; set; }
}
