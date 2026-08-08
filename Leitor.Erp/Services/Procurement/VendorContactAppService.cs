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

public class VendorContactAppService :
    CrudAppService<VendorContact, VendorContactDto, Guid, GetVendorContactListInput, CreateUpdateVendorContactDto>
{
    public VendorContactAppService(IRepository<VendorContact, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Vendors.Default;
        GetListPolicyName = ErpPermissions.Vendors.Default;
        CreatePolicyName = ErpPermissions.Vendors.Edit;
        UpdatePolicyName = ErpPermissions.Vendors.Edit;
        DeletePolicyName = ErpPermissions.Vendors.Edit;
    }

    protected override async Task<IQueryable<VendorContact>> CreateFilteredQueryAsync(GetVendorContactListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.VendorId.HasValue, x => x.VendorId == input.VendorId!.Value);
    }

    protected override Task<VendorContact> MapToEntityAsync(CreateUpdateVendorContactDto createInput)
    {
        var entity = new VendorContact(GuidGenerator.Create(), createInput.VendorId, createInput.FullName);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateVendorContactDto updateInput, VendorContact entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateVendorContactDto input, VendorContact entity)
    {
        entity.VendorId = input.VendorId;
        entity.FullName = input.FullName;
        entity.JobTitle = input.JobTitle;
        entity.Email = input.Email;
        entity.PhoneNumber = input.PhoneNumber;
        entity.IsPrimary = input.IsPrimary;
        entity.Notes = input.Notes;
    }
}