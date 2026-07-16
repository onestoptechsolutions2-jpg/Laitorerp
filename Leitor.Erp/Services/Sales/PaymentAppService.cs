using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class PaymentAppService :
    CrudAppService<Payment, PaymentDto, Guid, GetPaymentListInput, CreateUpdatePaymentDto>
{
    public PaymentAppService(IRepository<Payment, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    protected override async Task<IQueryable<Payment>> CreateFilteredQueryAsync(GetPaymentListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.InvoiceId.HasValue, x => x.InvoiceId == input.InvoiceId!.Value);
    }

    protected override Task<Payment> MapToEntityAsync(CreateUpdatePaymentDto createInput)
    {
        var entity = new Payment(GuidGenerator.Create(), createInput.InvoiceId, createInput.Amount, createInput.PaymentDate);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdatePaymentDto updateInput, Payment entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdatePaymentDto input, Payment entity)
    {
        entity.InvoiceId = input.InvoiceId;
        entity.Amount = input.Amount;
        entity.PaymentDate = input.PaymentDate;
        entity.Method = input.Method;
        entity.Reference = input.Reference;
        entity.Notes = input.Notes;
    }
}
