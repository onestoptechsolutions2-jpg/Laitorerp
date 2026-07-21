using System;
using Leitor.Erp.Entities.KnowledgeBase;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.KnowledgeBase;

public class KnowledgeArticleDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public KnowledgeArticleStatus Status { get; set; }
    public string? Tags { get; set; }
    public Guid? SourceTicketId { get; set; }

    // Resolved by KnowledgeArticleAppService - not a stored column.
    public string? SourceTicketNumber { get; set; }
}
