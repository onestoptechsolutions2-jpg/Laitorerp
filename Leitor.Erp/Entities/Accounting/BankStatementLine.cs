using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// One row per bank statement transaction, imported by pasting CSV text (Date,Description,Amount)
// rather than a file upload - no object storage needed for something this small. Amount is signed
// (positive = money in, negative = money out) so it nets directly against the linked GL account's
// Debit/Credit lines during reconciliation.
public class BankStatementLine : FullAuditedAggregateRoot<Guid>
{
    public Guid BankAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public bool IsReconciled { get; set; }
    public Guid? MatchedJournalEntryLineId { get; set; }

    protected BankStatementLine()
    {
    }

    public BankStatementLine(Guid id, Guid bankAccountId, DateTime transactionDate, string description, decimal amount)
        : base(id)
    {
        BankAccountId = bankAccountId;
        TransactionDate = transactionDate;
        Description = description;
        Amount = amount;
    }
}
