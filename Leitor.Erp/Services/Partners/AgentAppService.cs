using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Partners;

[RequiresFeature(ErpFeatures.PartnerCommission)]
public class AgentAppService :
    CrudAppService<Agent, AgentDto, Guid, GetAgentListInput, CreateUpdateAgentDto>
{
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Commission, Guid> _commissionRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public AgentAppService(
        IRepository<Agent, Guid> repository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Commission, Guid> commissionRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _opportunityRepository = opportunityRepository;
        _leadRepository = leadRepository;
        _commissionRepository = commissionRepository;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Partners.Default;
        GetListPolicyName = ErpPermissions.Partners.Default;
        CreatePolicyName = ErpPermissions.Partners.Create;
        UpdatePolicyName = ErpPermissions.Partners.Edit;
        DeletePolicyName = ErpPermissions.Partners.Delete;
    }

    // Same rationale as PartnerAppService.DeleteAsync: an Agent with recorded Commissions can't be
    // deleted (financial history must stay traceable), but the softer Opportunity.AgentId /
    // Lead.ReferrerAgentId tags are cleared so the referencing records survive.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Agent", id);

        var hasCommissions = (await _commissionRepository.GetListAsync(x => x.AgentId == id)).Count > 0;
        if (hasCommissions)
        {
            throw new UserFriendlyException("This agent has recorded commissions and can't be deleted.");
        }

        var opportunities = await _opportunityRepository.GetListAsync(x => x.AgentId == id);
        if (opportunities.Count > 0)
        {
            foreach (var opportunity in opportunities)
            {
                opportunity.AgentId = null;
            }

            await _opportunityRepository.UpdateManyAsync(opportunities);
        }

        var leads = await _leadRepository.GetListAsync(x => x.ReferrerAgentId == id);
        if (leads.Count > 0)
        {
            foreach (var lead in leads)
            {
                lead.ReferrerAgentId = null;
            }

            await _leadRepository.UpdateManyAsync(leads);
        }

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Agent>> CreateFilteredQueryAsync(GetAgentListInput input)
    {
        input.Sorting ??= $"{nameof(Agent.Name)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!));
    }

    protected override Task<Agent> MapToEntityAsync(CreateUpdateAgentDto createInput)
    {
        var entity = new Agent(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateAgentDto updateInput, Agent entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateAgentDto input, Agent entity)
    {
        entity.Name = input.Name;
        entity.Email = input.Email;
        entity.Phone = input.Phone;
        entity.Territory = input.Territory;
        entity.Skills = input.Skills;
        entity.Notes = input.Notes;
        entity.CommissionBasis = input.CommissionBasis;
        entity.CommissionRate = input.CommissionRate;
        entity.CommissionTrigger = input.CommissionTrigger;
    }
}
