using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class InvoiceDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }

    // Resolved/computed by InvoiceAppService - not stored columns.
    public string? CustomerName { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }

    // Computed exactly like Manager.io: not a manually-set field. See InvoicePaymentStatus.
    public InvoicePaymentStatus PaymentStatus { get; set; }
}
