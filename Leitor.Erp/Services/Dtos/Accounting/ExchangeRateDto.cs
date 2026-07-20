using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class ExchangeRateDto : FullAuditedEntityDto<Guid>
{
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime RateDate { get; set; }
    public decimal RateToBaseCurrency { get; set; }
    public string Source { get; set; } = string.Empty;
}
