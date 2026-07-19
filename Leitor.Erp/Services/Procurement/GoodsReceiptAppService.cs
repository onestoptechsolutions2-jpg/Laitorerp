using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

// Not a CrudAppService: a single receiving event covers multiple PurchaseOrderLines at once, which
// doesn't fit the generic single-entity Create shape - same reasoning WorkflowMonitorAppService
// uses for being a plain ApplicationService. This is the "goods received" leg of the three-way
// match (PO / Receipt / Supplier Invoice); CreateAsync is also the only place that ever flips a
// PurchaseOrder to Received, and only once every line's cumulative received quantity has actually
// reached what was ordered.
public class GoodsReceiptAppService : ApplicationService
{
    private readonly IRepository<GoodsReceipt, Guid> _repository;
    private readonly IRepository<GoodsReceiptLine, Guid> _lineRepository;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;

    public GoodsReceiptAppService(
        IRepository<GoodsReceipt, Guid> repository,
        IRepository<GoodsReceiptLine, Guid> lineRepository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository)
    {
        _repository = repository;
        _lineRepository = lineRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
    }

    public async Task<List<GoodsReceiptDto>> GetListAsync(GetGoodsReceiptListInput input)
    {
        await CheckPolicyAsync(ErpPermissions.Procurement.Default);

        var receipts = input.PurchaseOrderId.HasValue
            ? await _repository.GetListAsync(x => x.PurchaseOrderId == input.PurchaseOrderId.Value)
            : await _repository.GetListAsync();
        receipts = receipts.OrderByDescending(x => x.ReceivedDate).ToList();

        var receiptIds = receipts.Select(x => x.Id).ToList();
        var allLines = receiptIds.Count > 0
            ? await _lineRepository.GetListAsync(x => receiptIds.Contains(x.GoodsReceiptId))
            : new List<GoodsReceiptLine>();
        var linesByReceiptId = allLines.ToLookup(x => x.GoodsReceiptId);

        var poLineIds = allLines.Select(x => x.PurchaseOrderLineId).Distinct().ToList();
        var poLineDescriptionsById = poLineIds.Count > 0
            ? (await _purchaseOrderLineRepository.GetListAsync(x => poLineIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Description)
            : new Dictionary<Guid, string>();

        return receipts.Select(receipt => new GoodsReceiptDto
        {
            Id = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            ReceivedDate = receipt.ReceivedDate,
            Notes = receipt.Notes,
            CreationTime = receipt.CreationTime,
            CreatorId = receipt.CreatorId,
            Lines = linesByReceiptId[receipt.Id].Select(line => new GoodsReceiptLineDto
            {
                Id = line.Id,
                GoodsReceiptId = line.GoodsReceiptId,
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                QuantityReceived = line.QuantityReceived,
                PurchaseOrderLineDescription = poLineDescriptionsById.GetValueOrDefault(line.PurchaseOrderLineId)
            }).ToList()
        }).ToList();
    }

    public async Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Procurement.Edit);

        var lines = input.Lines.Where(x => x.QuantityReceived > 0).ToList();
        if (lines.Count == 0)
        {
            throw new UserFriendlyException("At least one line with a quantity received is required.");
        }

        var poLines = await _purchaseOrderLineRepository.GetListAsync(x => x.PurchaseOrderId == input.PurchaseOrderId);
        var poLinesById = poLines.ToDictionary(x => x.Id);
        var poLineIds = poLines.Select(x => x.Id).ToList();

        var existingReceiptLines = poLineIds.Count > 0
            ? await _lineRepository.GetListAsync(x => poLineIds.Contains(x.PurchaseOrderLineId))
            : new List<GoodsReceiptLine>();
        var receivedSoFarByLineId = existingReceiptLines
            .GroupBy(x => x.PurchaseOrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityReceived));

        foreach (var line in lines)
        {
            if (!poLinesById.TryGetValue(line.PurchaseOrderLineId, out var poLine))
            {
                throw new UserFriendlyException("One of the lines does not belong to this purchase order.");
            }

            var alreadyReceived = receivedSoFarByLineId.GetValueOrDefault(line.PurchaseOrderLineId);
            if (alreadyReceived + line.QuantityReceived > poLine.Quantity)
            {
                throw new UserFriendlyException(
                    $"Receiving {line.QuantityReceived} of \"{poLine.Description}\" would exceed the ordered quantity ({poLine.Quantity}; {alreadyReceived} already received).");
            }
        }

        var receipt = new GoodsReceipt(GuidGenerator.Create(), input.PurchaseOrderId, input.ReceivedDate)
        {
            Notes = input.Notes
        };
        await _repository.InsertAsync(receipt, autoSave: true);

        var lineDtos = new List<GoodsReceiptLineDto>();
        foreach (var line in lines)
        {
            var receiptLine = new GoodsReceiptLine(GuidGenerator.Create(), receipt.Id, line.PurchaseOrderLineId, line.QuantityReceived);
            await _lineRepository.InsertAsync(receiptLine, autoSave: true);
            receivedSoFarByLineId[line.PurchaseOrderLineId] = receivedSoFarByLineId.GetValueOrDefault(line.PurchaseOrderLineId) + line.QuantityReceived;

            lineDtos.Add(new GoodsReceiptLineDto
            {
                Id = receiptLine.Id,
                GoodsReceiptId = receipt.Id,
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                QuantityReceived = line.QuantityReceived,
                PurchaseOrderLineDescription = poLinesById[line.PurchaseOrderLineId].Description
            });
        }

        var isFullyReceived = poLines.All(poLine => receivedSoFarByLineId.GetValueOrDefault(poLine.Id) >= poLine.Quantity);
        if (isFullyReceived)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetAsync(input.PurchaseOrderId);
            purchaseOrder.Status = PurchaseOrderStatus.Received;
            await _purchaseOrderRepository.UpdateAsync(purchaseOrder, autoSave: true);
        }

        return new GoodsReceiptDto
        {
            Id = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            ReceivedDate = receipt.ReceivedDate,
            Notes = receipt.Notes,
            CreationTime = receipt.CreationTime,
            Lines = lineDtos
        };
    }
}
