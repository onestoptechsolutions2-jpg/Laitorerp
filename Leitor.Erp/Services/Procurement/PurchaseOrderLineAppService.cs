using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class PurchaseOrderLineAppService :
    CrudAppService<PurchaseOrderLine, PurchaseOrderLineDto, Guid, GetPurchaseOrderLineListInput, CreateUpdatePurchaseOrderLineDto>
{
    public PurchaseOrderLineAppService(IRepository<PurchaseOrderLine, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<PurchaseOrderLine>> CreateFilteredQueryAsync(GetPurchaseOrderLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.PurchaseOrderId.HasValue, x => x.PurchaseOrderId == input.PurchaseOrderId!.Value);
    }

    public override async Task<PagedResultDto<PurchaseOrderLineDto>> GetListAsync(GetPurchaseOrderLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<PurchaseOrderLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(PurchaseOrderLineDto dto)
    {
        dto.LineTotal = dto.UnitPrice * dto.Quantity * (1 - dto.DiscountPercent / 100m);
    }

    protected override Task<PurchaseOrderLine> MapToEntityAsync(CreateUpdatePurchaseOrderLineDto createInput)
    {
        var entity = new PurchaseOrderLine(GuidGenerator.Create(), createInput.PurchaseOrderId, createInput.Description, createInput.UnitPrice);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdatePurchaseOrderLineDto updateInput, PurchaseOrderLine entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdatePurchaseOrderLineDto input, PurchaseOrderLine entity)
    {
        entity.PurchaseOrderId = input.PurchaseOrderId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;
    }
}
