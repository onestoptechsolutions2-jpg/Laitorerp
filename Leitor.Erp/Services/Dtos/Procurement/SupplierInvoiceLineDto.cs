using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class SupplierInvoiceLineDto : FullAuditedEntityDto<Guid>
{
    public Guid SupplierInvoiceId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountPercent { get; set; }

    public decimal LineTotal { get; set; }
}
