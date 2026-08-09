using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Partners;

[RequiresFeature(ErpFeatures.PartnerCommission)]
public class CommissionAppService :
    CrudAppService<Commission, CommissionDto, Guid, GetCommissionListInput, CreateUpdateCommissionDto>
{
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Partner, Guid> _partnerRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public CommissionAppService(
        IRepository<Commission, Guid> repository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Partner, Guid> partnerRepository,
        IRepository<Agent, Guid> agentRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _opportunityRepository = opportunityRepository;
        _partnerRepository = partnerRepository;
        _agentRepository = agentRepository;
        _stageEventRepository = stageEventRepository;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Partners.Default;
        GetListPolicyName = ErpPermissions.Partners.Default;
        CreatePolicyName = ErpPermissions.Partners.Create;
        UpdatePolicyName = ErpPermissions.Partners.Edit;
        DeletePolicyName = ErpPermissions.Partners.Delete;
    }

    // A financial record - same governance level as Invoice/Vendor, not left to routine
    // permission-only deletion (see TC-046).
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Commission", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Commission>> CreateFilteredQueryAsync(GetCommissionListInput input)
    {
        input.Sorting ??= $"{nameof(Commission.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.OpportunityId.HasValue, x => x.OpportunityId == input.OpportunityId!.Value)
            .WhereIf(input.PartnerId.HasValue, x => x.PartnerId == input.PartnerId!.Value)
            .WhereIf(input.AgentId.HasValue, x => x.AgentId == input.AgentId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value);
    }

    public override async Task<CommissionDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<CommissionDto>> GetListAsync(GetCommissionListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<CommissionDto> items)
    {
        var opportunityIds = items.Select(x => x.OpportunityId).Distinct().ToList();
        var opportunityNamesById = opportunityIds.Count > 0
            ? (await _opportunityRepository.GetListAsync(x => opportunityIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var partnerIds = items.Where(x => x.PartnerId.HasValue).Select(x => x.PartnerId!.Value).Distinct().ToList();
        var partnerNamesById = partnerIds.Count > 0
            ? (await _partnerRepository.GetListAsync(x => partnerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var agentIds = items.Where(x => x.AgentId.HasValue).Select(x => x.AgentId!.Value).Distinct().ToList();
        var agentNamesById = agentIds.Count > 0
            ? (await _agentRepository.GetListAsync(x => agentIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var item in items)
        {
            if (opportunityNamesById.TryGetValue(item.OpportunityId, out var opportunityName))
            {
                item.OpportunityName = opportunityName;
            }

            if (item.PartnerId.HasValue && partnerNamesById.TryGetValue(item.PartnerId.Value, out var partnerName))
            {
                item.PartnerName = partnerName;
            }

            if (item.AgentId.HasValue && agentNamesById.TryGetValue(item.AgentId.Value, out var agentName))
            {
                item.AgentName = agentName;
            }
        }
    }

    protected override async Task<Commission> MapToEntityAsync(CreateUpdateCommissionDto createInput)
    {
        ValidateExactlyOnePartyIsSet(createInput);

        var entity = new Commission(GuidGenerator.Create(), createInput.OpportunityId);
        CopyToEntity(createInput, entity);

        // OnProposalAccepted commissions are payable the instant they're recorded - there's no
        // later trigger event to wait for, unlike OnClientPayment (see CommissionAutoPayableService).
        if (entity.Trigger == CommissionTrigger.OnProposalAccepted)
        {
            entity.Status = CommissionStatus.Payable;
            await WorkflowStageLog.RecordAsync(
                _stageEventRepository, GuidGenerator, CurrentUser, Clock,
                "Commission", entity.Id, WorkflowStage.CommissionAccrued, notes: "Payable on proposal acceptance");
        }

        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateCommissionDto updateInput, Commission entity)
    {
        ValidateExactlyOnePartyIsSet(updateInput);
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    // Only a Partners.Edit holder can record that a Payable commission has actually been paid out -
    // the system never assumes money left the business on its own (see CommissionStatus.cs).
    public async Task<CommissionDto> MarkPaidAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Partners.Edit);

        var entity = await Repository.GetAsync(id);
        if (entity.Status != CommissionStatus.Payable)
        {
            throw new UserFriendlyException("This commission isn't payable yet - it can't be marked paid.");
        }

        entity.Status = CommissionStatus.Paid;
        entity.PaidDate = Clock.Now;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Commission", entity.Id, WorkflowStage.CommissionPaid);

        var dto = await MapToGetOutputDtoAsync(entity);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    private static void ValidateExactlyOnePartyIsSet(CreateUpdateCommissionDto input)
    {
        var partnerSet = input.PartnerId.HasValue;
        var agentSet = input.AgentId.HasValue;

        if (partnerSet == agentSet)
        {
            throw new UserFriendlyException("A commission must be owed to exactly one Partner or one Agent, not both or neither.");
        }
    }

    private static void CopyToEntity(CreateUpdateCommissionDto input, Commission entity)
    {
        entity.OpportunityId = input.OpportunityId;
        entity.PartnerId = input.PartnerId;
        entity.AgentId = input.AgentId;
        entity.Basis = input.Basis;
        entity.Rate = input.Rate;
        entity.BaseAmount = input.BaseAmount;
        entity.Trigger = input.Trigger;
        entity.SourceInvoiceId = input.SourceInvoiceId;
        entity.Notes = input.Notes;

        // Fixed is a flat payout amount (Rate read directly); every other basis is a percentage
        // applied against BaseAmount - see CommissionBasis.cs.
        entity.Amount = input.Basis == CommissionBasis.Fixed
            ? input.Rate
            : Math.Round(input.BaseAmount * input.Rate / 100m, 2);
    }
}
