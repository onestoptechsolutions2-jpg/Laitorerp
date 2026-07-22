using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class PurchaseOrderAppService :
    CrudAppService<PurchaseOrder, PurchaseOrderDto, Guid, GetPurchaseOrderListInput, CreateUpdatePurchaseOrderDto>
{
    private readonly IRepository<PurchaseOrderLine, Guid> _lineRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly IRepository<GoodsReceiptLine, Guid> _goodsReceiptLineRepository;
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<VendorPayment, Guid> _vendorPaymentRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public PurchaseOrderAppService(
        IRepository<PurchaseOrder, Guid> repository,
        IRepository<PurchaseOrderLine, Guid> lineRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        IRepository<GoodsReceiptLine, Guid> goodsReceiptLineRepository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository,
        IRepository<VendorPayment, Guid> vendorPaymentRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _vendorRepository = vendorRepository;
        _orderRepository = orderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _goodsReceiptLineRepository = goodsReceiptLineRepository;
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
        _vendorPaymentRepository = vendorPaymentRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _dataFilter = dataFilter;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Create;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Delete;
    }

    // PurchaseOrderLines/GoodsReceipts/SupplierInvoices are independent aggregate roots with no FK
    // relationship configured, so deleting a PO doesn't cascade automatically - same pattern as
    // OrderAppService.DeleteAsync/InvoiceAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "PurchaseOrder", id);

        var lines = await _lineRepository.GetListAsync(x => x.PurchaseOrderId == id);
        await _lineRepository.DeleteManyAsync(lines);

        var receipts = await _goodsReceiptRepository.GetListAsync(x => x.PurchaseOrderId == id);
        if (receipts.Count > 0)
        {
            var receiptIds = receipts.Select(x => x.Id).ToList();
            await _goodsReceiptLineRepository.DeleteManyAsync(await _goodsReceiptLineRepository.GetListAsync(x => receiptIds.Contains(x.GoodsReceiptId)));
            await _goodsReceiptRepository.DeleteManyAsync(receipts);
        }

        var supplierInvoices = await _supplierInvoiceRepository.GetListAsync(x => x.PurchaseOrderId == id);
        if (supplierInvoices.Count > 0)
        {
            var supplierInvoiceIds = supplierInvoices.Select(x => x.Id).ToList();
            await _supplierInvoiceLineRepository.DeleteManyAsync(await _supplierInvoiceLineRepository.GetListAsync(x => supplierInvoiceIds.Contains(x.SupplierInvoiceId)));
            await _vendorPaymentRepository.DeleteManyAsync(await _vendorPaymentRepository.GetListAsync(x => supplierInvoiceIds.Contains(x.SupplierInvoiceId)));
            await _supplierInvoiceRepository.DeleteManyAsync(supplierInvoices);
        }

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

        var poLineIds = allLines.Select(x => x.Id).ToList();
        var receivedByPoLineId = poLineIds.Count > 0
            ? (await _goodsReceiptLineRepository.GetListAsync(x => poLineIds.Contains(x.PurchaseOrderLineId)))
                .GroupBy(x => x.PurchaseOrderLineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityReceived))
            : new Dictionary<Guid, decimal>();

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

            var lines = linesByPurchaseOrderId[purchaseOrder.Id].ToList();
            purchaseOrder.Total = lines.Sum(x => x.Subtotal());

            var orderedQuantity = lines.Sum(x => x.Quantity);
            var receivedQuantity = lines.Sum(x => Math.Min(receivedByPoLineId.GetValueOrDefault(x.Id), x.Quantity));
            purchaseOrder.ReceivedPercent = orderedQuantity > 0 ? receivedQuantity / orderedQuantity * 100m : 0;
            purchaseOrder.IsFullyReceived = lines.Count > 0 && lines.All(x => receivedByPoLineId.GetValueOrDefault(x.Id) >= x.Quantity);
        }
    }

    // CreateUpdatePurchaseOrderDto -> PurchaseOrder is mapped manually rather than via Mapperly -
    // same reason as every other entity in this app (protected Id setter).
    protected override async Task<PurchaseOrder> MapToEntityAsync(CreateUpdatePurchaseOrderDto createInput)
    {
        var poNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "PO-");

        var entity = new PurchaseOrder(GuidGenerator.Create(), createInput.VendorId, poNumber);
        CopyToEntity(createInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.OrderDate);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdatePurchaseOrderDto updateInput, PurchaseOrder entity)
    {
        CopyToEntity(updateInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.OrderDate);
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
        entity.CurrencyCode = input.CurrencyCode;
    }
}
