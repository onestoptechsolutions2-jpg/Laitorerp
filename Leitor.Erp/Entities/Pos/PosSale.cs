using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Pos;

// A completed-at-the-till sale, paid in full at the moment of sale (no separate Invoice/Payment
// step - see PosSaleAppService.CompleteSaleAsync, which posts straight to Cash/Revenue/COGS the
// same way OrderAppService.OnOrderFulfilledAsync posts COGS). CustomerId is nullable - a walk-in
// sale with no linked Customer is the common case at a physical till.
public class PosSale : FullAuditedAggregateRoot<Guid>
{
    public string SaleNumber { get; set; } = string.Empty;
    public Guid PosSessionId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid SalespersonUserId { get; set; }
    public DateTime SaleDate { get; set; }
    public PosSaleStatus Status { get; set; } = PosSaleStatus.Completed;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;
    public string? Notes { get; set; }

    protected PosSale()
    {
    }

    public PosSale(Guid id, string saleNumber, Guid posSessionId, Guid warehouseId, Guid salespersonUserId, DateTime saleDate)
        : base(id)
    {
        SaleNumber = saleNumber;
        PosSessionId = posSessionId;
        WarehouseId = warehouseId;
        SalespersonUserId = salespersonUserId;
        SaleDate = saleDate;
    }
}
