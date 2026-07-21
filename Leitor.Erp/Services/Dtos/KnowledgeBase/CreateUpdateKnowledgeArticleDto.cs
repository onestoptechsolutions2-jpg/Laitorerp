using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.KnowledgeBase;

namespace Leitor.Erp.Services.Dtos.KnowledgeBase;

public class CreateUpdateKnowledgeArticleDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public KnowledgeArticleStatus Status { get; set; } = KnowledgeArticleStatus.Draft;

    [StringLength(512)]
    public string? Tags { get; set; }

    public Guid? SourceTicketId { get; set; }
}
