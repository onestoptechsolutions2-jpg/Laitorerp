using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateTaxRateDto
{
    [Required]
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal Percent { get; set; }

    public bool IsDefault { get; set; }

    public TaxType TaxType { get; set; } = TaxType.Vat;
}
