using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.KnowledgeBase;

// ITIL4 Knowledge Management: a reusable solution, promotable from a resolved Ticket (see the
// "Promote to KB" action on Ticket Detail) so a recurring fix doesn't get re-diagnosed from
// scratch every time. SourceTicketId is a loose reference (no FK) purely for traceability back to
// where the article originated - editable and useful long after that Ticket is closed/deleted.
public class KnowledgeArticle : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public KnowledgeArticleStatus Status { get; set; } = KnowledgeArticleStatus.Draft;
    public string? Tags { get; set; }
    public Guid? SourceTicketId { get; set; }

    protected KnowledgeArticle()
    {
    }

    public KnowledgeArticle(Guid id, string title, string body)
        : base(id)
    {
        Title = title;
        Body = body;
    }
}
