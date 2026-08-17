using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Hr;

public class CreateUpdateNssfTierDto
{
    [Range(1, 10)]
    public int TierNumber { get; set; }

    [Range(0, double.MaxValue)]
    public decimal LowerBound { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UpperBound { get; set; }

    [Range(0, 100)]
    public decimal EmployeeRate { get; set; }

    [Range(0, 100)]
    public decimal EmployerRate { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }
}
