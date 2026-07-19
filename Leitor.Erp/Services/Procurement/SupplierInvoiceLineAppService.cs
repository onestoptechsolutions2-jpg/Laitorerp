using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class SupplierInvoiceLineAppService :
    CrudAppService<SupplierInvoiceLine, SupplierInvoiceLineDto, Guid, GetSupplierInvoiceLineListInput, CreateUpdateSupplierInvoiceLineDto>
{
    public SupplierInvoiceLineAppService(IRepository<SupplierInvoiceLine, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<SupplierInvoiceLine>> CreateFilteredQueryAsync(GetSupplierInvoiceLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.SupplierInvoiceId.HasValue, x => x.SupplierInvoiceId == input.SupplierInvoiceId!.Value);
    }

    public override async Task<PagedResultDto<SupplierInvoiceLineDto>> GetListAsync(GetSupplierInvoiceLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<SupplierInvoiceLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(SupplierInvoiceLineDto dto)
    {
        dto.LineTotal = dto.UnitPrice * dto.Quantity * (1 - dto.DiscountPercent / 100m);
    }

    protected override Task<SupplierInvoiceLine> MapToEntityAsync(CreateUpdateSupplierInvoiceLineDto createInput)
    {
        var entity = new SupplierInvoiceLine(GuidGenerator.Create(), createInput.SupplierInvoiceId, createInput.Description, createInput.UnitPrice);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateSupplierInvoiceLineDto updateInput, SupplierInvoiceLine entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateSupplierInvoiceLineDto input, SupplierInvoiceLine entity)
    {
        entity.SupplierInvoiceId = input.SupplierInvoiceId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;
    }
}
