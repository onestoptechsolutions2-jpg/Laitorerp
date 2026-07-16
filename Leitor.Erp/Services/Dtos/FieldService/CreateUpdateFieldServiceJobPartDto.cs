using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class CreateUpdateFieldServiceJobPartDto
{
    [Required]
    public Guid JobId { get; set; }

    public Guid? ProductId { get; set; }

    [Required]
    [StringLength(512)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;
}
