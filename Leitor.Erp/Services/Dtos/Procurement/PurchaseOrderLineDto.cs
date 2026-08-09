using System;
using Leitor.Erp.Entities.Common;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class PurchaseOrderLineDto : FullAuditedEntityDto<Guid>, ILineItem
{
    public Guid PurchaseOrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    // Computed by PurchaseOrderLineAppService - not a stored column.
    public decimal LineTotal { get; set; }
}
