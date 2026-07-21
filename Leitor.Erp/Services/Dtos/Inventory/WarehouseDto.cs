using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Inventory;

public class WarehouseDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
