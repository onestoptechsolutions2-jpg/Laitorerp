using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateUpdateCurrencyDto
{
    [Required]
    [StringLength(8)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(8)]
    public string Symbol { get; set; } = string.Empty;

    public bool IsBaseCurrency { get; set; }

    public bool IsActive { get; set; } = true;
}
