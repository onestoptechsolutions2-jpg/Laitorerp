using Leitor.Erp.Entities.KnowledgeBase;
using Leitor.Erp.Services.Dtos.KnowledgeBase;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.KnowledgeBase;

[Mapper]
public partial class KnowledgeArticleToKnowledgeArticleDtoMapper : MapperBase<KnowledgeArticle, KnowledgeArticleDto>
{
    [MapperIgnoreSource(nameof(KnowledgeArticle.ExtraProperties))]
    [MapperIgnoreSource(nameof(KnowledgeArticle.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(KnowledgeArticleDto.SourceTicketNumber))]
    public override partial KnowledgeArticleDto Map(KnowledgeArticle source);

    [MapperIgnoreSource(nameof(KnowledgeArticle.ExtraProperties))]
    [MapperIgnoreSource(nameof(KnowledgeArticle.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(KnowledgeArticleDto.SourceTicketNumber))]
    public override partial void Map(KnowledgeArticle source, KnowledgeArticleDto destination);
}
