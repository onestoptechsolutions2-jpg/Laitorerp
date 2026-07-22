using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class BankAccountDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid LinkedGlAccountId { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime OpeningBalanceDate { get; set; }

    // Resolved by BankAccountAppService from the Account repository - not a stored column.
    public string? LinkedGlAccountName { get; set; }

    // Computed live from JournalEntryLines posted to LinkedGlAccountId, never stored - same
    // "compute, never store" discipline as every other balance in this app.
    public decimal GlBalance { get; set; }
    public int UnreconciledStatementLineCount { get; set; }
}
