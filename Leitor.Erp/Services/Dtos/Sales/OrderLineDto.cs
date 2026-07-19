using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class OrderLineDto : FullAuditedEntityDto<Guid>
{
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    public decimal LineTotal { get; set; }
    public decimal? MarginPercent { get; set; }
}
