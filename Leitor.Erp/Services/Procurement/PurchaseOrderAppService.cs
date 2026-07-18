using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class PurchaseOrderAppService :
    CrudAppService<PurchaseOrder, PurchaseOrderDto, Guid, GetPurchaseOrderListInput, CreateUpdatePurchaseOrderDto>
{
    private readonly IRepository<PurchaseOrderLine, Guid> _lineRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Order, Guid> _orderRepository;

    public PurchaseOrderAppService(
        IRepository<PurchaseOrder, Guid> repository,
        IRepository<PurchaseOrderLine, Guid> lineRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Order, Guid> orderRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _vendorRepository = vendorRepository;
        _orderRepository = orderRepository;

        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Create;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Delete;
    }

    // PurchaseOrderLines are an independent aggregate root with no FK relationship configured, so
    // deleting a PO doesn't cascade automatically - same pattern as OrderAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        var lines = await _lineRepository.GetListAsync(x => x.PurchaseOrderId == id);
        await _lineRepository.DeleteManyAsync(lines);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<PurchaseOrder>> CreateFilteredQueryAsync(GetPurchaseOrderListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.VendorId.HasValue, x => x.VendorId == input.VendorId!.Value)
            .WhereIf(input.SourceOrderId.HasValue, x => x.SourceOrderId == input.SourceOrderId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.PONumber.Contains(input.Filter!));
    }

    public override async Task<PurchaseOrderDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<PurchaseOrderDto>> GetListAsync(GetPurchaseOrderListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<PurchaseOrderDto> purchaseOrders)
    {
        var vendorIds = purchaseOrders.Select(x => x.VendorId).Distinct().ToList();
        var vendors = await _vendorRepository.GetListAsync(x => vendorIds.Contains(x.Id));
        var namesById = vendors.ToDictionary(x => x.Id, x => x.Name);

        var purchaseOrderIds = purchaseOrders.Select(x => x.Id).ToList();
        var allLines = await _lineRepository.GetListAsync(x => purchaseOrderIds.Contains(x.PurchaseOrderId));
        var linesByPurchaseOrderId = allLines.ToLookup(x => x.PurchaseOrderId);

        var sourceOrderIds = purchaseOrders
            .Where(x => x.SourceOrderId.HasValue)
            .Select(x => x.SourceOrderId!.Value)
            .Distinct()
            .ToList();
        var orderNumbersById = sourceOrderIds.Count > 0
            ? (await _orderRepository.GetListAsync(x => sourceOrderIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.OrderNumber)
            : new Dictionary<Guid, string>();

        foreach (var purchaseOrder in purchaseOrders)
        {
            if (namesById.TryGetValue(purchaseOrder.VendorId, out var vendorName))
            {
                purchaseOrder.VendorName = vendorName;
            }

            if (purchaseOrder.SourceOrderId.HasValue &&
                orderNumbersById.TryGetValue(purchaseOrder.SourceOrderId.Value, out var orderNumber))
            {
                purchaseOrder.SourceOrderNumber = orderNumber;
            }

            purchaseOrder.Total = linesByPurchaseOrderId[purchaseOrder.Id]
                .Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
        }
    }

    // CreateUpdatePurchaseOrderDto -> PurchaseOrder is mapped manually rather than via Mapperly -
    // same reason as every other entity in this app (protected Id setter).
    protected override async Task<PurchaseOrder> MapToEntityAsync(CreateUpdatePurchaseOrderDto createInput)
    {
        var count = await Repository.GetCountAsync();
        var poNumber = $"PO-{count + 1:D6}";

        var entity = new PurchaseOrder(GuidGenerator.Create(), createInput.VendorId, poNumber);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdatePurchaseOrderDto updateInput, PurchaseOrder entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdatePurchaseOrderDto input, PurchaseOrder entity)
    {
        entity.VendorId = input.VendorId;
        entity.Status = input.Status;
        entity.OrderDate = input.OrderDate;
        entity.ExpectedDeliveryDate = input.ExpectedDeliveryDate;
        entity.Notes = input.Notes;
        entity.SourceOrderId = input.SourceOrderId;
        entity.ShipToCustomer = input.ShipToCustomer;
    }
}
