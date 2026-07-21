using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class JournalEntryLineDto : FullAuditedEntityDto<Guid>
{
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;
    public Guid? ProjectId { get; set; }

    // Resolved by JournalEntryAppService - not stored columns.
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public string? ProjectNumber { get; set; }
}
