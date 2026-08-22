using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Documents;

// A stable, unauthenticated-friendly public link to one generated document PDF - the "smart
// document link" shared via WhatsApp/email instead of a raw attachment (see
// Services/Documents/DocumentShareLinkService, Services/Documents/PublicDocumentResolver, and
// Pages/Documents/Index). The PDF is never stored here - it's regenerated live from current data
// every time the link is opened, the same way the existing "Download PDF" buttons already work,
// so a shared link never goes stale even if the underlying document is edited afterwards. The
// entity's own Id doubles as the public token (an unguessable Guid) rather than a separate Token
// column - one less thing to keep in sync.
public class DocumentShareLink : CreationAuditedEntity<Guid>
{
    public string DocumentType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    protected DocumentShareLink()
    {
    }

    public DocumentShareLink(Guid id, string documentType, Guid entityId)
        : base(id)
    {
        DocumentType = documentType;
        EntityId = entityId;
    }
}
