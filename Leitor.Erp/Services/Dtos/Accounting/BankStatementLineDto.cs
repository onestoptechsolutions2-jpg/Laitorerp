using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class BankStatementLineDto : FullAuditedEntityDto<Guid>
{
    public Guid BankAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public bool IsReconciled { get; set; }
    public Guid? MatchedJournalEntryLineId { get; set; }
}

// A row on the GL side of the reconciliation screen - a JournalEntryLine posted to the bank
// account's linked GL account, not yet matched to any BankStatementLine.
public class UnreconciledGlLineDto
{
    public Guid JournalEntryLineId { get; set; }
    public DateTime EntryDate { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class BankReconciliationSummaryDto
{
    public decimal GlBalance { get; set; }
    public decimal ReconciledStatementBalance { get; set; }
    public decimal Difference { get; set; }
}
