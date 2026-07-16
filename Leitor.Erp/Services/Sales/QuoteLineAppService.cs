using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class QuoteLineAppService :
    CrudAppService<QuoteLine, QuoteLineDto, Guid, GetQuoteLineListInput, CreateUpdateQuoteLineDto>
{
    public QuoteLineAppService(IRepository<QuoteLine, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    protected override async Task<IQueryable<QuoteLine>> CreateFilteredQueryAsync(GetQuoteLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.QuoteId.HasValue, x => x.QuoteId == input.QuoteId!.Value);
    }

    public override async Task<PagedResultDto<QuoteLineDto>> GetListAsync(GetQuoteLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<QuoteLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(QuoteLineDto dto)
    {
        dto.LineTotal = dto.UnitPrice * dto.Quantity * (1 - dto.DiscountPercent / 100m);
    }

    protected override Task<QuoteLine> MapToEntityAsync(CreateUpdateQuoteLineDto createInput)
    {
        var entity = new QuoteLine(GuidGenerator.Create(), createInput.QuoteId, createInput.Description, createInput.UnitPrice);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateQuoteLineDto updateInput, QuoteLine entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateQuoteLineDto input, QuoteLine entity)
    {
        entity.QuoteId = input.QuoteId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;
    }
}
