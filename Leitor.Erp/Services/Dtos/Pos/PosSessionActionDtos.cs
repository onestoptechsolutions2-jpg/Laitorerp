using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Pos;

public class OpenPosSessionDto
{
    [Required]
    public Guid WarehouseId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OpeningCashAmount { get; set; }
}

public class ClosePosSessionDto
{
    [Range(0, double.MaxValue)]
    public decimal ClosingCashAmount { get; set; }
}

public class ProductSearchInputDto
{
    public string Term { get; set; } = string.Empty;

    [Required]
    public Guid WarehouseId { get; set; }
}
