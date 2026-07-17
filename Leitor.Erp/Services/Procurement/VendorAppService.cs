using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class VendorAppService :
    CrudAppService<Vendor, VendorDto, Guid, GetVendorListInput, CreateUpdateVendorDto>
{
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;

    public VendorAppService(
        IRepository<Vendor, Guid> repository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository)
        : base(repository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;

        GetPolicyName = ErpPermissions.Vendors.Default;
        GetListPolicyName = ErpPermissions.Vendors.Default;
        CreatePolicyName = ErpPermissions.Vendors.Create;
        UpdatePolicyName = ErpPermissions.Vendors.Edit;
        DeletePolicyName = ErpPermissions.Vendors.Delete;
    }

    // PurchaseOrders are an independent aggregate root with no FK relationship configured, so
    // deleting a vendor doesn't cascade automatically - same pattern as CustomerAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        var purchaseOrders = await _purchaseOrderRepository.GetListAsync(x => x.VendorId == id);
        if (purchaseOrders.Count > 0)
        {
            var purchaseOrderIds = purchaseOrders.Select(x => x.Id).ToList();
            await _purchaseOrderLineRepository.DeleteManyAsync(
                await _purchaseOrderLineRepository.GetListAsync(x => purchaseOrderIds.Contains(x.PurchaseOrderId)));
            await _purchaseOrderRepository.DeleteManyAsync(purchaseOrders);
        }

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Vendor>> CreateFilteredQueryAsync(GetVendorListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!));
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
    }
}
