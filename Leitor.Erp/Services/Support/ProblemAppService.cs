using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Support;

public class ProblemAppService :
    CrudAppService<Problem, ProblemDto, Guid, GetProblemListInput, CreateUpdateProblemDto>
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IClock _clock;
    private readonly IDataFilter _dataFilter;

    public ProblemAppService(
        IRepository<Problem, Guid> repository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IClock clock,
        IDataFilter dataFilter)
        : base(repository)
    {
        _ticketRepository = ticketRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _clock = clock;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Support.Default;
        GetListPolicyName = ErpPermissions.Support.Default;
        CreatePolicyName = ErpPermissions.Support.Create;
        UpdatePolicyName = ErpPermissions.Support.Edit;
        DeletePolicyName = ErpPermissions.Support.Delete;
    }

    // Matches every other top-level entity with its own Index/Delete button (Ticket,
    // WarrantyClaim, etc.) - immediate delete requires DeletionApprovals.Decide, everyone else
    // files a request. Tickets referencing this Problem keep their ProblemId (a loose reference,
    // not an FK) rather than being cascade-updated - same "loose reference" shape DeletionRequest
    // itself uses.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Problem", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Problem>> CreateFilteredQueryAsync(GetProblemListInput input)
    {
        input.Sorting ??= $"{nameof(Problem.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Title.Contains(input.Filter!) || x.ProblemNumber.Contains(input.Filter!));
    }

    public override async Task<ProblemDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ProblemDto>> GetListAsync(GetProblemListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ProblemDto> problems)
    {
        var problemIds = problems.Select(x => x.Id).ToList();
        var countsByProblemId = problemIds.Count > 0
            ? (await _ticketRepository.GetListAsync(x => x.ProblemId.HasValue && problemIds.Contains(x.ProblemId.Value)))
                .GroupBy(x => x.ProblemId!.Value)
                .ToDictionary(g => g.Key, g => g.Count())
            : new Dictionary<Guid, int>();

        foreach (var problem in problems)
        {
            problem.LinkedTicketCount = countsByProblemId.GetValueOrDefault(problem.Id);
        }
    }

    protected override async Task<Problem> MapToEntityAsync(CreateUpdateProblemDto createInput)
    {
        var problemNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "PRB-");

        var entity = new Problem(GuidGenerator.Create(), problemNumber, createInput.Title);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateProblemDto updateInput, Problem entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateProblemDto input, Problem entity)
    {
        entity.Title = input.Title;
        entity.Description = input.Description;
        entity.RootCause = input.RootCause;
        entity.Workaround = input.Workaround;
        entity.IdentifiedDate = input.IdentifiedDate;

        // Resolved/Closed are terminal - same auto-tracking pattern as Ticket.ResolvedDate/
        // WarrantyClaim.ResolvedDate.
        var wasTerminal = entity.Status is ProblemStatus.Resolved or ProblemStatus.Closed;
        var isTerminal = input.Status is ProblemStatus.Resolved or ProblemStatus.Closed;

        if (isTerminal && !wasTerminal)
        {
            entity.ResolvedDate = _clock.Now;
        }
        else if (!isTerminal)
        {
            entity.ResolvedDate = null;
        }

        entity.Status = input.Status;
    }
}
