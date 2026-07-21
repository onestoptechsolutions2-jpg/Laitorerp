using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class CreateGoodsReceiptDto
{
    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public DateTime ReceivedDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Optional - defaults to whichever Warehouse has IsDefault set when left blank (see
    // GoodsReceiptAppService.CreateAsync).
    public Guid? WarehouseId { get; set; }

    public List<CreateGoodsReceiptLineDto> Lines { get; set; } = new();
}

public class CreateGoodsReceiptLineDto
{
    [Required]
    public Guid PurchaseOrderLineId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal QuantityReceived { get; set; }
}
