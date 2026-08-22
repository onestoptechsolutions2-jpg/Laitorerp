namespace Leitor.Erp.Entities.Documents;

// Plain string constants rather than an enum - same "SourceDocumentType" convention already used
// by JournalPostingService's idempotency keys, so a new document type never needs a migration to
// add (just a new PublicDocumentResolver branch).
public static class DocumentShareType
{
    public const string Quote = "Quote";
    public const string Order = "Order";
    public const string Invoice = "Invoice";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string Proposal = "Proposal";
}
