using System;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class SupplierInvoiceDto : FullAuditedEntityDto<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public Guid VendorId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public SupplierInvoiceStatus Status { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }

    // Resolved/computed by SupplierInvoiceAppService - not stored columns, Mapperly won't map them.
    public string? VendorName { get; set; }
    public string? PONumber { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public InvoicePaymentStatus PaymentStatus { get; set; }
}
