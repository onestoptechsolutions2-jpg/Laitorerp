using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Partners;

// The Partner directory itself is core (see MODULES.md) - only Commission math is the toggleable
// PartnerCommission feature (see CommissionAppService). Not feature-gated here.
public class PartnerAppService :
    CrudAppService<Partner, PartnerDto, Guid, GetPartnerListInput, CreateUpdatePartnerDto>
{
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Commission, Guid> _commissionRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IRepository<ProjectTask, Guid> _projectTaskRepository;
    private readonly IRepository<ServiceCatalogItem, Guid> _serviceCatalogItemRepository;

    public PartnerAppService(
        IRepository<Partner, Guid> repository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Commission, Guid> commissionRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IRepository<ProjectTask, Guid> projectTaskRepository,
        IRepository<ServiceCatalogItem, Guid> serviceCatalogItemRepository)
        : base(repository)
    {
        _opportunityRepository = opportunityRepository;
        _commissionRepository = commissionRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _projectTaskRepository = projectTaskRepository;
        _serviceCatalogItemRepository = serviceCatalogItemRepository;

        GetPolicyName = ErpPermissions.Partners.Default;
        GetListPolicyName = ErpPermissions.Partners.Default;
        CreatePolicyName = ErpPermissions.Partners.Create;
        UpdatePolicyName = ErpPermissions.Partners.Edit;
        DeletePolicyName = ErpPermissions.Partners.Delete;
    }

    // A Partner with recorded Commissions (even Paid ones) can't be deleted - those rows are
    // financial history and must always stay traceable to who they're owed to (see TC-046, "users
    // must not be able to silently alter historical transactions"). Opportunity.PartnerId is a
    // much softer "who's involved" tag, so it's cleared instead - same nullable-FK-on-a-surviving-
    // record pattern VendorAppService.DeleteAsync uses for FieldServiceJob.VendorId.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Partner", id);

        var hasCommissions = (await _commissionRepository.GetListAsync(x => x.PartnerId == id)).Count > 0;
        if (hasCommissions)
        {
            throw new UserFriendlyException("This partner has recorded commissions and can't be deleted.");
        }

        var opportunities = await _opportunityRepository.GetListAsync(x => x.PartnerId == id);
        if (opportunities.Count > 0)
        {
            foreach (var opportunity in opportunities)
            {
                opportunity.PartnerId = null;
            }

            await _opportunityRepository.UpdateManyAsync(opportunities);
        }

        // Same soft-reference-clearing rationale as Opportunity above, closing a gap where these
        // two were previously left pointing at a deleted Partner (a dangling reference, not just a
        // UX nicety - see the UX/error-handling audit's DependencyGuard-gap pass).
        var projectTasks = await _projectTaskRepository.GetListAsync(x => x.PartnerId == id);
        if (projectTasks.Count > 0)
        {
            foreach (var task in projectTasks)
            {
                task.PartnerId = null;
            }

            await _projectTaskRepository.UpdateManyAsync(projectTasks);
        }

        var serviceCatalogItems = await _serviceCatalogItemRepository.GetListAsync(x => x.PartnerId == id);
        if (serviceCatalogItems.Count > 0)
        {
            foreach (var item in serviceCatalogItems)
            {
                item.PartnerId = null;
            }

            await _serviceCatalogItemRepository.UpdateManyAsync(serviceCatalogItems);
        }

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Partner>> CreateFilteredQueryAsync(GetPartnerListInput input)
    {
        input.Sorting ??= $"{nameof(Partner.Name)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.Category.HasValue, x => x.Category == input.Category!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value);
    }

    protected override Task<Partner> MapToEntityAsync(CreateUpdatePartnerDto createInput)
    {
        var entity = new Partner(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdatePartnerDto updateInput, Partner entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdatePartnerDto input, Partner entity)
    {
        entity.Name = input.Name;
        entity.Category = input.Category;
        entity.Email = input.Email;
        entity.Phone = input.Phone;
        entity.Notes = input.Notes;
        entity.IsActive = input.IsActive;
        entity.CommissionBasis = input.CommissionBasis;
        entity.CommissionRate = input.CommissionRate;
        entity.CommissionTrigger = input.CommissionTrigger;
    }
}
