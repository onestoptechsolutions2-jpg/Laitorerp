using System;
using Leitor.Erp.Entities.Inventory;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Inventory;

public class StockMovementDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal Quantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }

    public string? ProductName { get; set; }
    public string? WarehouseName { get; set; }
}
