using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class GetBankAccountListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
