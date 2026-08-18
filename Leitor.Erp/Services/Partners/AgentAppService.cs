using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Calendar;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Partners;

// The Agent directory itself is core (see MODULES.md) - only Commission math is the toggleable
// PartnerCommission feature (see CommissionAppService). Not feature-gated here.
public class AgentAppService :
    CrudAppService<Agent, AgentDto, Guid, GetAgentListInput, CreateUpdateAgentDto>
{
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Commission, Guid> _commissionRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IRepository<CalendarEvent, Guid> _calendarEventRepository;
    private readonly IRepository<ProjectTask, Guid> _projectTaskRepository;

    public AgentAppService(
        IRepository<Agent, Guid> repository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Commission, Guid> commissionRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IRepository<CalendarEvent, Guid> calendarEventRepository,
        IRepository<ProjectTask, Guid> projectTaskRepository)
        : base(repository)
    {
        _opportunityRepository = opportunityRepository;
        _leadRepository = leadRepository;
        _commissionRepository = commissionRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _calendarEventRepository = calendarEventRepository;
        _projectTaskRepository = projectTaskRepository;

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

        // Same soft-reference-clearing rationale as Opportunity/Lead above, closing a gap where
        // these two were previously left pointing at a deleted Agent (a dangling reference, not
        // just a UX nicety - see the UX/error-handling audit's DependencyGuard-gap pass).
        var calendarEvents = await _calendarEventRepository.GetListAsync(x => x.AgentId == id);
        if (calendarEvents.Count > 0)
        {
            foreach (var calendarEvent in calendarEvents)
            {
                calendarEvent.AgentId = null;
            }

            await _calendarEventRepository.UpdateManyAsync(calendarEvents);
        }

        var projectTasks = await _projectTaskRepository.GetListAsync(x => x.AgentId == id);
        if (projectTasks.Count > 0)
        {
            foreach (var task in projectTasks)
            {
                task.AgentId = null;
            }

            await _projectTaskRepository.UpdateManyAsync(projectTasks);
        }

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Agent>> CreateFilteredQueryAsync(GetAgentListInput input)
    {
        input.Sorting ??= $"{nameof(Agent.Name)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value);
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
        entity.IsActive = input.IsActive;
        entity.CommissionBasis = input.CommissionBasis;
        entity.CommissionRate = input.CommissionRate;
        entity.CommissionTrigger = input.CommissionTrigger;
    }
}
