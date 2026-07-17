using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class PurchaseOrderLineDto : FullAuditedEntityDto<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountPercent { get; set; }

    // Computed by PurchaseOrderLineAppService - not a stored column.
    public decimal LineTotal { get; set; }
}
