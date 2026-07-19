using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Opportunities;

public class ProposalAppService :
    CrudAppService<Proposal, ProposalDto, Guid, GetProposalListInput, CreateUpdateProposalDto>
{
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IDataFilter _dataFilter;

    public ProposalAppService(
        IRepository<Proposal, Guid> repository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _opportunityRepository = opportunityRepository;
        _quoteRepository = quoteRepository;
        _stageEventRepository = stageEventRepository;
        _dataFilter = dataFilter;

        // Child reuses the parent Opportunity's permissions, same convention as
        // NeedsAssessmentAppService - Proposals are always managed from the Opportunity Detail
        // page, never their own top-level list.
        GetPolicyName = ErpPermissions.Opportunities.Default;
        GetListPolicyName = ErpPermissions.Opportunities.Default;
        CreatePolicyName = ErpPermissions.Opportunities.Edit;
        UpdatePolicyName = ErpPermissions.Opportunities.Edit;
        DeletePolicyName = ErpPermissions.Opportunities.Edit;
    }

    protected override async Task<IQueryable<Proposal>> CreateFilteredQueryAsync(GetProposalListInput input)
    {
        input.Sorting ??= $"{nameof(Proposal.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.OpportunityId.HasValue, x => x.OpportunityId == input.OpportunityId!.Value);
    }

    protected override async Task<Proposal> MapToEntityAsync(CreateUpdateProposalDto createInput)
    {
        var proposalNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "PROP-");

        var entity = new Proposal(GuidGenerator.Create(), createInput.OpportunityId, proposalNumber, createInput.Title);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateProposalDto updateInput, Proposal entity)
    {
        // Once a Proposal leaves Draft it's locked - editing requires an explicit unlock first
        // (see UnlockForRevisionAsync). The unlock is single-use: it's consumed the moment this
        // edit is saved, so a fresh unlock is required for every subsequent change.
        if (entity.IsLocked && entity.UnlockedByUserId == null)
        {
            throw new UserFriendlyException("This proposal is locked because it's no longer a draft. Unlock it for revision before making changes.");
        }

        // A resend after edits is a new revision of the same document - bump Version whenever the
        // content changes via update, not on every save (e.g. a pure Status flip Draft->Sent isn't
        // a new revision).
        var contentChanged =
            entity.Summary != updateInput.Summary ||
            entity.ProposedSolution != updateInput.ProposedSolution ||
            entity.Scope != updateInput.Scope ||
            entity.Timeline != updateInput.Timeline ||
            entity.Assumptions != updateInput.Assumptions ||
            entity.Exclusions != updateInput.Exclusions ||
            entity.WarrantyAndSupport != updateInput.WarrantyAndSupport ||
            entity.Terms != updateInput.Terms;

        var wasUnlocked = entity.UnlockedByUserId != null;

        CopyToEntity(updateInput, entity);

        if (contentChanged)
        {
            entity.Version++;
        }

        if (wasUnlocked)
        {
            entity.UnlockedByUserId = null;
            entity.UnlockedAt = null;
            entity.UnlockReason = null;
        }

        return Task.CompletedTask;
    }

    // Only a holder of Opportunities.Unlock (Ops Manager) can unlock an approved Proposal for
    // revision - the reason is mandatory and logged, and the unlock is consumed by the very next
    // saved edit (see MapToEntityAsync above).
    public async Task UnlockForRevisionAsync(Guid id, string reason)
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Unlock);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("A reason is required to unlock this proposal for revision.");
        }

        var entity = await Repository.GetAsync(id);
        entity.UnlockedByUserId = CurrentUser.Id;
        entity.UnlockedAt = Clock.Now;
        entity.UnlockReason = reason;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Proposal", entity.Id, WorkflowStage.Unlocked, notes: reason);
    }

    // Called after the PDF is actually emailed (Channel: "Email") or after a staff member confirms
    // they've sent it manually over WhatsApp (Channel: "WhatsApp", log-only - no API integration,
    // per the workflow-governance scope decision). Draft -> Sent on the first delivery of either
    // kind; later re-sends (e.g. after a revision) just add another history entry.
    public async Task MarkSentAsync(Guid id, string channel)
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Edit);

        var proposal = await Repository.GetAsync(id);
        if (proposal.Status == ProposalStatus.Draft)
        {
            proposal.Status = ProposalStatus.Sent;
            await Repository.UpdateAsync(proposal, autoSave: true);
        }

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Proposal", proposal.Id, WorkflowStage.ProposalSent, channel: channel);
    }

    public Task<List<WorkflowStageEvent>> GetDeliveryHistoryAsync(Guid id)
    {
        return WorkflowStageLog.GetHistoryAsync(_stageEventRepository, "Proposal", id);
    }

    private static void CopyToEntity(CreateUpdateProposalDto input, Proposal entity)
    {
        entity.OpportunityId = input.OpportunityId;
        entity.Title = input.Title;
        entity.Status = input.Status;
        entity.Summary = input.Summary;
        entity.ProposedSolution = input.ProposedSolution;
        entity.Scope = input.Scope;
        entity.Timeline = input.Timeline;
        entity.Assumptions = input.Assumptions;
        entity.Exclusions = input.Exclusions;
        entity.WarrantyAndSupport = input.WarrantyAndSupport;
        entity.Terms = input.Terms;
    }

    // The concrete mechanism behind "proposal becomes a quotation" - same shape as
    // QuoteAppService.ConvertToOrderAsync, except there are no line items to carry forward: the
    // Quote's own lines ARE the proposal's de facto Bill of Materials (see Proposal.cs).
    // Conversion IS the approval action (sets Status to Accepted below) rather than requiring
    // Accepted beforehand - matches the same pattern QuoteAppService.ConvertToOrderAsync already
    // uses. What IS blocked: converting a Proposal the customer already rejected, and converting
    // the same Proposal twice (one Proposal -> one Quote).
    public async Task<QuoteDto> ConvertToQuoteAsync(Guid proposalId)
    {
        await CheckCreatePolicyAsync();

        var proposal = await Repository.GetAsync(proposalId);

        if (proposal.Status == ProposalStatus.Rejected)
        {
            throw new UserFriendlyException("This proposal was rejected and can't be converted to a quote.");
        }

        var alreadyConverted = (await _quoteRepository.GetListAsync(x => x.ProposalId == proposal.Id)).Any();
        if (alreadyConverted)
        {
            throw new UserFriendlyException("This proposal has already been converted to a quote.");
        }

        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var quoteNumber = await DocumentNumbering.NextAsync(_quoteRepository, _dataFilter, "Q-");

        var quote = new Quote(GuidGenerator.Create(), opportunity.CustomerId, quoteNumber, proposal.Title)
        {
            ProposalId = proposal.Id,
            IssueDate = Clock.Now
        };
        await _quoteRepository.InsertAsync(quote, autoSave: true);

        proposal.Status = ProposalStatus.Accepted;
        await Repository.UpdateAsync(proposal, autoSave: true);
        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Proposal", proposal.Id, WorkflowStage.ProposalApproved);

        return ObjectMapper.Map<Quote, QuoteDto>(quote);
    }
}
