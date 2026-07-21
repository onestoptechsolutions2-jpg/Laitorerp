using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Inventory;

public class CreateUpdateWarehouseDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Address { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
