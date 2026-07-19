using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
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

public class WarrantyClaimAppService :
    CrudAppService<WarrantyClaim, WarrantyClaimDto, Guid, GetWarrantyClaimListInput, CreateUpdateWarrantyClaimDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IClock _clock;
    private readonly IDataFilter _dataFilter;

    public WarrantyClaimAppService(
        IRepository<WarrantyClaim, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IClock clock,
        IDataFilter dataFilter)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _clock = clock;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Support.Default;
        GetListPolicyName = ErpPermissions.Support.Default;
        CreatePolicyName = ErpPermissions.Support.Create;
        UpdatePolicyName = ErpPermissions.Support.Edit;
        DeletePolicyName = ErpPermissions.Support.Delete;
    }

    // Matches every other top-level entity with its own Index/Delete button (Ticket, PurchaseOrder,
    // etc.) - immediate delete requires DeletionApprovals.Decide, everyone else files a request.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "WarrantyClaim", id);
        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<WarrantyClaim>> CreateFilteredQueryAsync(GetWarrantyClaimListInput input)
    {
        input.Sorting ??= $"{nameof(WarrantyClaim.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.ClaimNumber.Contains(input.Filter!) || x.Description.Contains(input.Filter!));
    }

    public override async Task<WarrantyClaimDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<WarrantyClaimDto>> GetListAsync(GetWarrantyClaimListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<WarrantyClaimDto> claims)
    {
        var customerIds = claims.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var namesById = customers.ToDictionary(x => x.Id, x => x.Name);

        foreach (var claim in claims)
        {
            if (namesById.TryGetValue(claim.CustomerId, out var customerName))
            {
                claim.CustomerName = customerName;
            }
        }
    }

    protected override async Task<WarrantyClaim> MapToEntityAsync(CreateUpdateWarrantyClaimDto createInput)
    {
        var claimNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "WC-");

        var entity = new WarrantyClaim(GuidGenerator.Create(), createInput.CustomerId, claimNumber, createInput.Description);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateWarrantyClaimDto updateInput, WarrantyClaim entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateWarrantyClaimDto input, WarrantyClaim entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.ContractId = input.ContractId;
        entity.JobId = input.JobId;
        entity.TicketId = input.TicketId;
        entity.Description = input.Description;
        entity.FiledDate = input.FiledDate;

        // Rejected/Resolved are terminal; Open/Approved are still active work - same auto-tracking
        // pattern as Ticket.ResolvedDate.
        var wasTerminal = entity.Status is WarrantyClaimStatus.Rejected or WarrantyClaimStatus.Resolved;
        var isTerminal = input.Status is WarrantyClaimStatus.Rejected or WarrantyClaimStatus.Resolved;

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
