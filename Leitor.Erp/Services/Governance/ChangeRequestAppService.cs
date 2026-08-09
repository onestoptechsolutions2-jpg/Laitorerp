using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Governance;

[RequiresFeature(ErpFeatures.ChangeEnablement)]
public class ChangeRequestAppService :
    CrudAppService<ChangeRequest, ChangeRequestDto, Guid, GetChangeRequestListInput, CreateUpdateChangeRequestDto>
{
    private readonly IRepository<ConfigurationItem, Guid> _configurationItemRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IDataFilter _dataFilter;

    public ChangeRequestAppService(
        IRepository<ChangeRequest, Guid> repository,
        IRepository<ConfigurationItem, Guid> configurationItemRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _configurationItemRepository = configurationItemRepository;
        _identityUserRepository = identityUserRepository;
        _stageEventRepository = stageEventRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Changes.Default;
        GetListPolicyName = ErpPermissions.Changes.Default;
        CreatePolicyName = ErpPermissions.Changes.Create;
        UpdatePolicyName = ErpPermissions.Changes.Edit;
        DeletePolicyName = ErpPermissions.Changes.Delete;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "ChangeRequest", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<ChangeRequest>> CreateFilteredQueryAsync(GetChangeRequestListInput input)
    {
        input.Sorting ??= $"{nameof(ChangeRequest.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.ConfigurationItemId.HasValue, x => x.ConfigurationItemId == input.ConfigurationItemId!.Value);
    }

    public override async Task<ChangeRequestDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ChangeRequestDto>> GetListAsync(GetChangeRequestListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    protected override async Task<ChangeRequest> MapToEntityAsync(CreateUpdateChangeRequestDto createInput)
    {
        // Confirms the CI is real rather than trusting whatever the dropdown posted - same
        // validate-the-FK discipline as ProposalAppService's SupersedesProposalId check.
        await _configurationItemRepository.GetAsync(createInput.ConfigurationItemId);

        var changeNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "CHG-");
        var entity = new ChangeRequest(GuidGenerator.Create(), createInput.ConfigurationItemId, changeNumber, createInput.Title)
        {
            Description = createInput.Description,
            Tier = createInput.Tier,
            TicketId = createInput.TicketId,
            PlannedDate = createInput.PlannedDate
        };

        // The whole point of the 3-tier model: only Normal changes wait on anyone. Standard is
        // pre-authorized by definition; Emergency proceeds immediately and gets a mandatory
        // retrospective review instead of an upfront gate (see ReviewPostImplementationAsync).
        entity.Status = createInput.Tier == ChangeTier.Normal
            ? ChangeRequestStatus.PendingApproval
            : ChangeRequestStatus.Approved;

        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateChangeRequestDto updateInput, ChangeRequest entity)
    {
        entity.Title = updateInput.Title;
        entity.Description = updateInput.Description;
        entity.TicketId = updateInput.TicketId;
        entity.PlannedDate = updateInput.PlannedDate;
        return Task.CompletedTask;
    }

    public async Task<ChangeRequestDto> ApproveAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Changes.Approve);

        var entity = await Repository.GetAsync(id);
        if (entity.Status != ChangeRequestStatus.PendingApproval)
        {
            throw new UserFriendlyException("This change isn't pending approval.");
        }

        entity.Status = ChangeRequestStatus.Approved;
        entity.ApprovedByUserId = CurrentUser.Id;
        entity.ApprovedDate = Clock.Now;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "ChangeRequest", entity.Id, WorkflowStage.ChangeApproved);

        var dto = await MapToGetOutputDtoAsync(entity);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public async Task<ChangeRequestDto> RejectAsync(Guid id, string reason)
    {
        await CheckPolicyAsync(ErpPermissions.Changes.Approve);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("A reason is required to reject a change request.");
        }

        var entity = await Repository.GetAsync(id);
        if (entity.Status != ChangeRequestStatus.PendingApproval)
        {
            throw new UserFriendlyException("This change isn't pending approval.");
        }

        entity.Status = ChangeRequestStatus.Rejected;
        entity.RejectionReason = reason;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "ChangeRequest", entity.Id, WorkflowStage.ChangeRejected, notes: reason);

        var dto = await MapToGetOutputDtoAsync(entity);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public async Task<ChangeRequestDto> CompleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Changes.Edit);

        var entity = await Repository.GetAsync(id);
        if (entity.Status != ChangeRequestStatus.Approved)
        {
            throw new UserFriendlyException("Only an approved change can be marked complete.");
        }

        entity.Status = ChangeRequestStatus.Completed;
        entity.CompletedDate = Clock.Now;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "ChangeRequest", entity.Id, WorkflowStage.ChangeCompleted);

        var dto = await MapToGetOutputDtoAsync(entity);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public async Task<ChangeRequestDto> RollBackAsync(Guid id, string notes)
    {
        await CheckPolicyAsync(ErpPermissions.Changes.Edit);

        if (string.IsNullOrWhiteSpace(notes))
        {
            throw new UserFriendlyException("A reason is required to record a rollback.");
        }

        var entity = await Repository.GetAsync(id);
        if (entity.Status != ChangeRequestStatus.Completed)
        {
            throw new UserFriendlyException("Only a completed change can be rolled back.");
        }

        entity.Status = ChangeRequestStatus.RolledBack;
        entity.RolledBack = true;
        entity.RollbackNotes = notes;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "ChangeRequest", entity.Id, WorkflowStage.ChangeRolledBack, notes: notes);

        var dto = await MapToGetOutputDtoAsync(entity);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    // Mandatory for Emergency-tier changes (they skip the upfront gate), harmless to call on any
    // other tier - just records that someone looked at it after the fact.
    public async Task<ChangeRequestDto> ReviewPostImplementationAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Changes.Approve);

        var entity = await Repository.GetAsync(id);
        entity.PostImplementationReviewedDate = Clock.Now;
        await Repository.UpdateAsync(entity, autoSave: true);

        var dto = await MapToGetOutputDtoAsync(entity);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ChangeRequestDto> changes)
    {
        var ciIds = changes.Select(x => x.ConfigurationItemId).Distinct().ToList();
        var ciNamesById = ciIds.Count > 0
            ? (await _configurationItemRepository.GetListAsync(x => ciIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var approverIds = changes.Where(x => x.ApprovedByUserId.HasValue).Select(x => x.ApprovedByUserId!.Value).Distinct().ToList();
        var approverNamesById = approverIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => approverIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        foreach (var change in changes)
        {
            if (ciNamesById.TryGetValue(change.ConfigurationItemId, out var ciName))
            {
                change.ConfigurationItemName = ciName;
            }

            if (change.ApprovedByUserId.HasValue && approverNamesById.TryGetValue(change.ApprovedByUserId.Value, out var approverName))
            {
                change.ApprovedByUserName = approverName;
            }
        }
    }
}
