using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GoodsReceiptLineDto : FullAuditedEntityDto<Guid>
{
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }

    // Resolved by GoodsReceiptAppService - not a stored column.
    public string? PurchaseOrderLineDescription { get; set; }
}
