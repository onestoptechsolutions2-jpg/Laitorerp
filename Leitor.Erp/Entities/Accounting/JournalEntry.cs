using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// A balanced double-entry transaction. SourceDocumentType/SourceDocumentId trace an
// auto-generated entry back to the Invoice/Payment/SupplierInvoice/VendorPayment that produced
// it (null for manual entries). IsSystemGenerated entries are never directly deletable/editable -
// only reversible via an equal-and-opposite entry (see JournalPostingService.ReverseAsync).
public class JournalEntry : FullAuditedAggregateRoot<Guid>
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public bool IsSystemGenerated { get; set; }

    protected JournalEntry()
    {
    }

    public JournalEntry(Guid id, string entryNumber, DateTime entryDate, string description)
        : base(id)
    {
        EntryNumber = entryNumber;
        EntryDate = entryDate;
        Description = description;
    }
}
