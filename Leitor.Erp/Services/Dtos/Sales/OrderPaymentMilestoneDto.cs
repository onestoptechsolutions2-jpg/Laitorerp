using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class OrderPaymentMilestoneDto : FullAuditedEntityDto<Guid>
{
    public Guid OrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public OrderPaymentMilestoneKind Kind { get; set; }
    public bool IsInvoiced { get; set; }
    public Guid? InvoiceId { get; set; }
}
