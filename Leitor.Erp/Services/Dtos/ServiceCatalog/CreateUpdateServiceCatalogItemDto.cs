using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.ServiceCatalog;

public class CreateUpdateServiceCatalogItemDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(128)]
    public string? Category { get; set; }

    public Guid? OwnerUserId { get; set; }

    [Range(1, 8760)]
    public int? TargetSlaHours { get; set; }

    public bool IsActive { get; set; } = true;
}
