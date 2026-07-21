using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GoodsReceiptDto : FullAuditedEntityDto<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public Guid WarehouseId { get; set; }

    // Resolved by GoodsReceiptAppService - not a stored column.
    public List<GoodsReceiptLineDto> Lines { get; set; } = new();
}
