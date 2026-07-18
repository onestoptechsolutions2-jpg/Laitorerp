using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Procurement;

public class VendorAppService :
    CrudAppService<Vendor, VendorDto, Guid, GetVendorListInput, CreateUpdateVendorDto>
{
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;
    private readonly IRepository<ProductVendor, Guid> _productVendorRepository;
    private readonly IRepository<FieldServiceJob, Guid> _fieldServiceJobRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public VendorAppService(
        IRepository<Vendor, Guid> repository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository,
        IRepository<ProductVendor, Guid> productVendorRepository,
        IRepository<FieldServiceJob, Guid> fieldServiceJobRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
        _productVendorRepository = productVendorRepository;
        _fieldServiceJobRepository = fieldServiceJobRepository;
        _identityUserRepository = identityUserRepository;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Vendors.Default;
        GetListPolicyName = ErpPermissions.Vendors.Default;
        CreatePolicyName = ErpPermissions.Vendors.Create;
        UpdatePolicyName = ErpPermissions.Vendors.Edit;
        DeletePolicyName = ErpPermissions.Vendors.Delete;
    }

    // PurchaseOrders and ProductVendor sourcing rows are independent aggregate roots with no FK
    // relationship configured, so deleting a vendor doesn't cascade automatically - same pattern
    // as CustomerAppService.DeleteAsync. FieldServiceJob.VendorId is nullable (a subcontracted
    // visit is still a real work record without its vendor), so it's cleared, not cascade-deleted.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Vendor", id);

        var purchaseOrders = await _purchaseOrderRepository.GetListAsync(x => x.VendorId == id);
        if (purchaseOrders.Count > 0)
        {
            var purchaseOrderIds = purchaseOrders.Select(x => x.Id).ToList();
            await _purchaseOrderLineRepository.DeleteManyAsync(
                await _purchaseOrderLineRepository.GetListAsync(x => purchaseOrderIds.Contains(x.PurchaseOrderId)));
            await _purchaseOrderRepository.DeleteManyAsync(purchaseOrders);
        }

        var productVendors = await _productVendorRepository.GetListAsync(x => x.VendorId == id);
        await _productVendorRepository.DeleteManyAsync(productVendors);

        var jobs = await _fieldServiceJobRepository.GetListAsync(x => x.VendorId == id);
        if (jobs.Count > 0)
        {
            foreach (var job in jobs)
            {
                job.VendorId = null;
            }

            await _fieldServiceJobRepository.UpdateManyAsync(jobs);
        }

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Vendor>> CreateFilteredQueryAsync(GetVendorListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!));
    }

    public override async Task<VendorDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolvePortalUserNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<VendorDto>> GetListAsync(GetVendorListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolvePortalUserNamesAsync(result.Items);
        return result;
    }

    private async Task ResolvePortalUserNamesAsync(IReadOnlyCollection<VendorDto> vendors)
    {
        var userIds = vendors
            .Where(x => x.PortalUserId.HasValue)
            .Select(x => x.PortalUserId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
        var namesById = users.ToDictionary(x => x.Id, x => x.UserName);

        foreach (var vendor in vendors)
        {
            if (vendor.PortalUserId.HasValue && namesById.TryGetValue(vendor.PortalUserId.Value, out var userName))
            {
                vendor.PortalUserName = userName;
            }
        }
    }

    // CreateUpdateVendorDto -> Vendor is mapped manually rather than via Mapperly - same reason as
    // every other entity in this app (protected Id setter + constructor args Mapperly can't
    // resolve from the DTO).
    protected override Task<Vendor> MapToEntityAsync(CreateUpdateVendorDto createInput)
    {
        var entity = new Vendor(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateVendorDto updateInput, Vendor entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateVendorDto input, Vendor entity)
    {
        entity.Name = input.Name;
        entity.Email = input.Email;
        entity.Phone = input.Phone;
        entity.AddressLine = input.AddressLine;
        entity.City = input.City;
        entity.State = input.State;
        entity.PostalCode = input.PostalCode;
        entity.Country = input.Country;
        entity.Notes = input.Notes;
        entity.PortalUserId = input.PortalUserId;
    }
}
