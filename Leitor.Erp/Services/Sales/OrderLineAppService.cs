using System;
using System.Collections.Generic;
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
    private readonly IRepository<ProductBundleItem, Guid> _bundleItemRepository;

    public OrderLineAppService(
        IRepository<OrderLine, Guid> repository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductBundleItem, Guid> bundleItemRepository)
        : base(repository)
    {
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;
        _bundleItemRepository = bundleItemRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    // Same bundle-explosion behavior as QuoteLineAppService.CreateAsync - see its comment for the
    // full rationale. Non-bundle lines (the overwhelming majority) fall straight through unchanged.
    public override async Task<OrderLineDto> CreateAsync(CreateUpdateOrderLineDto input)
    {
        await CheckCreatePolicyAsync();

        if (!input.ProductId.HasValue)
        {
            return await base.CreateAsync(input);
        }

        var product = await _productRepository.FindAsync(input.ProductId.Value);
        if (product is not { IsBundle: true })
        {
            return await base.CreateAsync(input);
        }

        var bundleItems = await _bundleItemRepository.GetListAsync(x => x.BundleProductId == product.Id);
        if (bundleItems.Count == 0)
        {
            throw new UserFriendlyException("This bundle has no components configured.");
        }

        OrderLine? lastLine = null;
        foreach (var bundleItem in bundleItems)
        {
            var component = await _productRepository.GetAsync(bundleItem.ComponentProductId);
            var (taxRateId, taxRatePercent) = await TaxRateResolver.ResolveAsync(_taxRateRepository, _productRepository, null, component.Id);

            var line = new OrderLine(GuidGenerator.Create(), input.OrderId, component.Name, component.UnitPrice)
            {
                ProductId = component.Id,
                Quantity = bundleItem.Quantity,
                Cost = component.Cost,
                TaxRateId = taxRateId,
                TaxRatePercent = taxRatePercent
            };
            await Repository.InsertAsync(line, autoSave: true);
            lastLine = line;
        }

        var dto = await MapToGetOutputDtoAsync(lastLine!);
        ComputeLineTotal(dto);
        return dto;
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
