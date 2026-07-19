using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class VendorPaymentAppService :
    CrudAppService<VendorPayment, VendorPaymentDto, Guid, GetVendorPaymentListInput, CreateUpdateVendorPaymentDto>
{
    public VendorPaymentAppService(IRepository<VendorPayment, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<VendorPayment>> CreateFilteredQueryAsync(GetVendorPaymentListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.SupplierInvoiceId.HasValue, x => x.SupplierInvoiceId == input.SupplierInvoiceId!.Value);
    }

    protected override Task<VendorPayment> MapToEntityAsync(CreateUpdateVendorPaymentDto createInput)
    {
        var entity = new VendorPayment(GuidGenerator.Create(), createInput.SupplierInvoiceId, createInput.Amount, createInput.PaymentDate);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateVendorPaymentDto updateInput, VendorPayment entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateVendorPaymentDto input, VendorPayment entity)
    {
        entity.SupplierInvoiceId = input.SupplierInvoiceId;
        entity.Amount = input.Amount;
        entity.PaymentDate = input.PaymentDate;
        entity.Method = input.Method;
        entity.Reference = input.Reference;
        entity.Notes = input.Notes;
    }
}
