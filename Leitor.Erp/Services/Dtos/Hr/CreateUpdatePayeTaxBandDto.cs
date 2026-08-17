using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Hr;

public class CreateUpdatePayeTaxBandDto
{
    [Range(0, double.MaxValue)]
    public decimal LowerBound { get; set; }

    public decimal? UpperBound { get; set; }

    [Range(0, 100)]
    public decimal Rate { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }
}
