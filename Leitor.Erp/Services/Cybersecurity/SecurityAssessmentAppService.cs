using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Cybersecurity;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Cybersecurity;

[RequiresFeature(ErpFeatures.Cybersecurity)]
public class SecurityAssessmentAppService :
    CrudAppService<SecurityAssessment, SecurityAssessmentDto, Guid, GetSecurityAssessmentListInput, CreateUpdateSecurityAssessmentDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IClock _clock;
    private readonly IDataFilter _dataFilter;

    public SecurityAssessmentAppService(
        IRepository<SecurityAssessment, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IClock clock,
        IDataFilter dataFilter)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _clock = clock;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Cybersecurity.Default;
        GetListPolicyName = ErpPermissions.Cybersecurity.Default;
        CreatePolicyName = ErpPermissions.Cybersecurity.Create;
        UpdatePolicyName = ErpPermissions.Cybersecurity.Edit;
        DeletePolicyName = ErpPermissions.Cybersecurity.Delete;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "SecurityAssessment", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<SecurityAssessment>> CreateFilteredQueryAsync(GetSecurityAssessmentListInput input)
    {
        input.Sorting ??= $"{nameof(SecurityAssessment.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Title.Contains(input.Filter!) || x.AssessmentNumber.Contains(input.Filter!));
    }

    public override async Task<SecurityAssessmentDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<SecurityAssessmentDto>> GetListAsync(GetSecurityAssessmentListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<SecurityAssessmentDto> assessments)
    {
        var customerIds = assessments.Select(x => x.CustomerId).Distinct().ToList();
        var customerNamesById = customerIds.Count > 0
            ? (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var userIds = assessments
            .Where(x => x.ConductedByUserId.HasValue)
            .Select(x => x.ConductedByUserId!.Value)
            .Distinct()
            .ToList();
        var usersById = userIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        foreach (var assessment in assessments)
        {
            if (customerNamesById.TryGetValue(assessment.CustomerId, out var customerName))
            {
                assessment.CustomerName = customerName;
            }

            if (assessment.ConductedByUserId.HasValue && usersById.TryGetValue(assessment.ConductedByUserId.Value, out var userName))
            {
                assessment.ConductedByUserName = userName;
            }
        }
    }

    protected override async Task<SecurityAssessment> MapToEntityAsync(CreateUpdateSecurityAssessmentDto createInput)
    {
        var assessmentNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "SEC-");

        var entity = new SecurityAssessment(GuidGenerator.Create(), createInput.CustomerId, assessmentNumber, createInput.Title);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateSecurityAssessmentDto updateInput, SecurityAssessment entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateSecurityAssessmentDto input, SecurityAssessment entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.Title = input.Title;
        entity.Type = input.Type;
        entity.ScheduledDate = input.ScheduledDate;
        entity.ConductedByUserId = input.ConductedByUserId;
        entity.RiskRating = input.RiskRating;
        entity.Findings = input.Findings;
        entity.Recommendations = input.Recommendations;
        entity.FollowUpDate = input.FollowUpDate;

        // Completed is the only terminal status - same auto-tracking pattern as
        // Problem.ResolvedDate/Ticket.ResolvedDate.
        var wasCompleted = entity.Status == SecurityAssessmentStatus.Completed;
        var isCompleted = input.Status == SecurityAssessmentStatus.Completed;

        if (isCompleted && !wasCompleted)
        {
            entity.CompletedDate = _clock.Now;
        }
        else if (!isCompleted)
        {
            entity.CompletedDate = null;
        }

        entity.Status = input.Status;
    }
}
