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

public class OrderLineAppService :
    CrudAppService<OrderLine, OrderLineDto, Guid, GetOrderLineListInput, CreateUpdateOrderLineDto>
{
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public OrderLineAppService(
        IRepository<OrderLine, Guid> repository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository)
        : base(repository)
    {
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    protected override async Task<IQueryable<OrderLine>> CreateFilteredQueryAsync(GetOrderLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.OrderId.HasValue, x => x.OrderId == input.OrderId!.Value);
    }

    public override async Task<PagedResultDto<OrderLineDto>> GetListAsync(GetOrderLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<OrderLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(OrderLineDto dto)
    {
        dto.LineTotal = dto.UnitPrice * dto.Quantity * (1 - dto.DiscountPercent / 100m);
        dto.MarginPercent = dto.UnitPrice > 0 ? Math.Round(100m * (dto.UnitPrice - dto.Cost) / dto.UnitPrice, 1) : null;
    }

    protected override async Task<OrderLine> MapToEntityAsync(CreateUpdateOrderLineDto createInput)
    {
        var entity = new OrderLine(GuidGenerator.Create(), createInput.OrderId, createInput.Description, createInput.UnitPrice);
        await CopyToEntityAsync(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateOrderLineDto updateInput, OrderLine entity)
    {
        await CopyToEntityAsync(updateInput, entity);
    }

    private async Task CopyToEntityAsync(CreateUpdateOrderLineDto input, OrderLine entity)
    {
        entity.OrderId = input.OrderId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;
        entity.Cost = input.Cost;

        (entity.TaxRateId, entity.TaxRatePercent) = await TaxRateResolver.ResolveAsync(
            _taxRateRepository, _productRepository, input.TaxRateId, input.ProductId);
    }
}
