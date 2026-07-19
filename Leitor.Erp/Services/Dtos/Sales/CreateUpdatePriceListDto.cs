using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdatePriceListDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;
}
