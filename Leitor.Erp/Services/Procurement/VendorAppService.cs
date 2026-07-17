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
    CrudAppService<Vendor, VendorDto, System.Guid, GetVendorListInput, CreateUpdateVendorDto>
{
    public VendorAppService(IRepository<Vendor, System.Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Vendors.Default;
        GetListPolicyName = ErpPermissions.Vendors.Default;
        CreatePolicyName = ErpPermissions.Vendors.Create;
        UpdatePolicyName = ErpPermissions.Vendors.Edit;
        DeletePolicyName = ErpPermissions.Vendors.Delete;
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
