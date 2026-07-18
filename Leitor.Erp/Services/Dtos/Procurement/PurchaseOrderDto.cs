using System;
using Leitor.Erp.Entities.Procurement;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class PurchaseOrderDto : FullAuditedEntityDto<Guid>
{
    public Guid VendorId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public Guid? SourceOrderId { get; set; }
    public bool ShipToCustomer { get; set; }

    // Resolved/computed by PurchaseOrderAppService - not stored columns, Mapperly won't map them.
    public string? VendorName { get; set; }
    public string? SourceOrderNumber { get; set; }
    public decimal Total { get; set; }
}
