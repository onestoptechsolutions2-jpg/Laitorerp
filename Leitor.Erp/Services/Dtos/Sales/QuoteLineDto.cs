using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class QuoteLineDto : FullAuditedEntityDto<Guid>
{
    public Guid QuoteId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    // Both computed by QuoteLineAppService - not stored columns.
    public decimal LineTotal { get; set; }
    public decimal? MarginPercent { get; set; }
}
