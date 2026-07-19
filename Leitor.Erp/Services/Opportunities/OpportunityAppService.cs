using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Opportunities;

public class OpportunityAppService :
    CrudAppService<Opportunity, OpportunityDto, Guid, GetOpportunityListInput, CreateUpdateOpportunityDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<NeedsAssessment, Guid> _needsAssessmentRepository;
    private readonly IRepository<NeedsAssessmentAttachment, Guid> _needsAssessmentAttachmentRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;

    public OpportunityAppService(
        IRepository<Opportunity, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<NeedsAssessment, Guid> needsAssessmentRepository,
        IRepository<NeedsAssessmentAttachment, Guid> needsAssessmentAttachmentRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<Lead, Guid> leadRepository)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _needsAssessmentRepository = needsAssessmentRepository;
        _needsAssessmentAttachmentRepository = needsAssessmentAttachmentRepository;
        _proposalRepository = proposalRepository;
        _leadRepository = leadRepository;

        GetPolicyName = ErpPermissions.Opportunities.Default;
        GetListPolicyName = ErpPermissions.Opportunities.Default;
        CreatePolicyName = ErpPermissions.Opportunities.Create;
        UpdatePolicyName = ErpPermissions.Opportunities.Edit;
        DeletePolicyName = ErpPermissions.Opportunities.Delete;
    }

    // NeedsAssessments/Proposals are independent aggregate roots with no FK relationship
    // configured, so deleting an Opportunity doesn't cascade automatically - same pattern as
    // CustomerAppService.DeleteAsync. Not gated by DeletionGate - pre-commitment pipeline
    // documents (Opportunity/NeedsAssessment/Proposal) follow the same precedent as Quote, which
    // also isn't approval-gated.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        var assessments = await _needsAssessmentRepository.GetListAsync(x => x.OpportunityId == id);
        if (assessments.Count > 0)
        {
            var assessmentIds = assessments.Select(x => x.Id).ToList();
            await _needsAssessmentAttachmentRepository.DeleteManyAsync(
                await _needsAssessmentAttachmentRepository.GetListAsync(x => assessmentIds.Contains(x.NeedsAssessmentId)));
            await _needsAssessmentRepository.DeleteManyAsync(assessments);
        }

        var proposals = await _proposalRepository.GetListAsync(x => x.OpportunityId == id);
        await _proposalRepository.DeleteManyAsync(proposals);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Opportunity>> CreateFilteredQueryAsync(GetOpportunityListInput input)
    {
        input.Sorting ??= $"{nameof(Opportunity.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(input.AssignedToUserId.HasValue, x => x.AssignedToUserId == input.AssignedToUserId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!));
    }

    public override async Task<OpportunityDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<OpportunityDto>> GetListAsync(GetOpportunityListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<OpportunityDto> opportunities)
    {
        var customerIds = opportunities.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var customerNamesById = customers.ToDictionary(x => x.Id, x => x.Name);

        var userIds = opportunities
            .Where(x => x.AssignedToUserId.HasValue)
            .Select(x => x.AssignedToUserId!.Value)
            .Distinct()
            .ToList();
        var usersById = userIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        var leadIds = opportunities
            .Where(x => x.LeadId.HasValue)
            .Select(x => x.LeadId!.Value)
            .Distinct()
            .ToList();
        var leadNamesById = leadIds.Count > 0
            ? (await _leadRepository.GetListAsync(x => leadIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        foreach (var opportunity in opportunities)
        {
            if (customerNamesById.TryGetValue(opportunity.CustomerId, out var customerName))
            {
                opportunity.CustomerName = customerName;
            }

            if (opportunity.AssignedToUserId.HasValue && usersById.TryGetValue(opportunity.AssignedToUserId.Value, out var userName))
            {
                opportunity.AssignedToUserName = userName;
            }

            if (opportunity.LeadId.HasValue && leadNamesById.TryGetValue(opportunity.LeadId.Value, out var leadName))
            {
                opportunity.LeadDisplayName = leadName;
            }
        }
    }

    // CreateUpdateOpportunityDto -> Opportunity is mapped manually rather than via Mapperly - same
    // reason as every other entity in this app (protected Id setter).
    protected override Task<Opportunity> MapToEntityAsync(CreateUpdateOpportunityDto createInput)
    {
        var entity = new Opportunity(GuidGenerator.Create(), createInput.CustomerId, createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateOpportunityDto updateInput, Opportunity entity)
    {
        // Auto-tracked the same way Ticket.ResolvedDate/FieldServiceJob.CompletedDate already are -
        // set the moment Status transitions into Won/Lost, cleared if reopened to Open. This is
        // what makes a real win-rate-over-time trend possible (see SalesAnalyticsAppService).
        var wasClosed = entity.Status is OpportunityStatus.Won or OpportunityStatus.Lost;
        var isClosed = updateInput.Status is OpportunityStatus.Won or OpportunityStatus.Lost;

        CopyToEntity(updateInput, entity);

        if (isClosed && !wasClosed)
        {
            entity.ClosedDate = Clock.Now;
        }
        else if (!isClosed)
        {
            entity.ClosedDate = null;
        }

        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateOpportunityDto input, Opportunity entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.Name = input.Name;
        entity.Status = input.Status;
        entity.EstimatedValue = input.EstimatedValue;
        entity.ExpectedCloseDate = input.ExpectedCloseDate;
        entity.AssignedToUserId = input.AssignedToUserId;
        entity.LostReason = input.LostReason;
        entity.Notes = input.Notes;
    }
}
