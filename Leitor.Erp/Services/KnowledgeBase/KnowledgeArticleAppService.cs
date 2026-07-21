using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.KnowledgeBase;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.KnowledgeBase;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.KnowledgeBase;

[RequiresFeature(ErpFeatures.KnowledgeManagement)]
public class KnowledgeArticleAppService :
    CrudAppService<KnowledgeArticle, KnowledgeArticleDto, Guid, GetKnowledgeArticleListInput, CreateUpdateKnowledgeArticleDto>
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public KnowledgeArticleAppService(
        IRepository<KnowledgeArticle, Guid> repository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _ticketRepository = ticketRepository;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.KnowledgeBase.Default;
        GetListPolicyName = ErpPermissions.KnowledgeBase.Default;
        CreatePolicyName = ErpPermissions.KnowledgeBase.Create;
        UpdatePolicyName = ErpPermissions.KnowledgeBase.Edit;
        DeletePolicyName = ErpPermissions.KnowledgeBase.Delete;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "KnowledgeArticle", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<KnowledgeArticle>> CreateFilteredQueryAsync(GetKnowledgeArticleListInput input)
    {
        input.Sorting ??= $"{nameof(KnowledgeArticle.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Title.Contains(input.Filter!) || x.Body.Contains(input.Filter!));
    }

    public override async Task<KnowledgeArticleDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<KnowledgeArticleDto>> GetListAsync(GetKnowledgeArticleListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<KnowledgeArticleDto> articles)
    {
        var ticketIds = articles.Where(x => x.SourceTicketId.HasValue).Select(x => x.SourceTicketId!.Value).Distinct().ToList();
        var numbersById = ticketIds.Count > 0
            ? (await _ticketRepository.GetListAsync(x => ticketIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.TicketNumber)
            : new Dictionary<Guid, string>();

        foreach (var article in articles)
        {
            if (article.SourceTicketId.HasValue && numbersById.TryGetValue(article.SourceTicketId.Value, out var ticketNumber))
            {
                article.SourceTicketNumber = ticketNumber;
            }
        }
    }

    protected override Task<KnowledgeArticle> MapToEntityAsync(CreateUpdateKnowledgeArticleDto createInput)
    {
        var entity = new KnowledgeArticle(GuidGenerator.Create(), createInput.Title, createInput.Body);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateKnowledgeArticleDto updateInput, KnowledgeArticle entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateKnowledgeArticleDto input, KnowledgeArticle entity)
    {
        entity.Title = input.Title;
        entity.Body = input.Body;
        entity.Status = input.Status;
        entity.Tags = input.Tags;
        entity.SourceTicketId = input.SourceTicketId;
    }
}
