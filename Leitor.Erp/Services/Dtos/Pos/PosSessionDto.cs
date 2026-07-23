using System;
using Leitor.Erp.Entities.Pos;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Pos;

public class PosSessionDto : FullAuditedEntityDto<Guid>
{
    public Guid WarehouseId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public PosSessionStatus Status { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal? ClosingCashAmount { get; set; }

    // Resolved by PosSessionAppService - not stored columns.
    public string? WarehouseName { get; set; }
    public string? OpenedByUserName { get; set; }

    // OpeningCashAmount + cash-tender PosPayments for sales in this session - computed live,
    // never stored, same "compute, never store" discipline as everything else in this app.
    public decimal ExpectedCashAmount { get; set; }
}
