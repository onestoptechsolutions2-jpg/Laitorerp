using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateUpdateExchangeRateDto
{
    [Required]
    [StringLength(8)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime RateDate { get; set; }

    [Range(0.000001, double.MaxValue)]
    public decimal RateToBaseCurrency { get; set; }
}
