using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Opportunities;

public class NeedsAssessmentAppService :
    CrudAppService<NeedsAssessment, NeedsAssessmentDto, Guid, GetNeedsAssessmentListInput, CreateUpdateNeedsAssessmentDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public NeedsAssessmentAppService(
        IRepository<NeedsAssessment, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;

        // Child reuses the parent Opportunity's permissions, per convention (e.g. CustomerContact
        // reusing Customers.Edit) - Needs Assessments are always managed from the Opportunity
        // Detail page, never their own top-level list.
        GetPolicyName = ErpPermissions.Opportunities.Default;
        GetListPolicyName = ErpPermissions.Opportunities.Default;
        CreatePolicyName = ErpPermissions.Opportunities.Edit;
        UpdatePolicyName = ErpPermissions.Opportunities.Edit;
        DeletePolicyName = ErpPermissions.Opportunities.Edit;
    }

    protected override async Task<IQueryable<NeedsAssessment>> CreateFilteredQueryAsync(GetNeedsAssessmentListInput input)
    {
        input.Sorting ??= $"{nameof(NeedsAssessment.ConductedDate)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.OpportunityId.HasValue, x => x.OpportunityId == input.OpportunityId!.Value);
    }

    public override async Task<NeedsAssessmentDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<NeedsAssessmentDto>> GetListAsync(GetNeedsAssessmentListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<NeedsAssessmentDto> assessments)
    {
        var userIds = assessments
            .Where(x => x.ConductedByUserId.HasValue)
            .Select(x => x.ConductedByUserId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
        var namesById = users.ToDictionary(x => x.Id, x => x.UserName);

        foreach (var assessment in assessments)
        {
            if (assessment.ConductedByUserId.HasValue && namesById.TryGetValue(assessment.ConductedByUserId.Value, out var userName))
            {
                assessment.ConductedByUserName = userName;
            }
        }
    }

    protected override Task<NeedsAssessment> MapToEntityAsync(CreateUpdateNeedsAssessmentDto createInput)
    {
        var entity = new NeedsAssessment(GuidGenerator.Create(), createInput.OpportunityId);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateNeedsAssessmentDto updateInput, NeedsAssessment entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateNeedsAssessmentDto input, NeedsAssessment entity)
    {
        entity.OpportunityId = input.OpportunityId;
        entity.Type = input.Type;
        entity.ConductedDate = input.ConductedDate;
        entity.ConductedByUserId = input.ConductedByUserId;
        entity.Findings = input.Findings;
        entity.Risks = input.Risks;
        entity.Recommendations = input.Recommendations;
        entity.CustomerRequirements = input.CustomerRequirements;
    }
}
