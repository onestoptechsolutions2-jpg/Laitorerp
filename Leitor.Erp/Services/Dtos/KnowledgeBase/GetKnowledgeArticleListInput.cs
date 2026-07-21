using Leitor.Erp.Entities.KnowledgeBase;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.KnowledgeBase;

public class GetKnowledgeArticleListInput : PagedAndSortedResultRequestDto
{
    public KnowledgeArticleStatus? Status { get; set; }
    public string? Filter { get; set; }
}
