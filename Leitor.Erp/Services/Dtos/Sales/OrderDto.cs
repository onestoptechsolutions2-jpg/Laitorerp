using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class OrderDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? QuoteId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public string? Notes { get; set; }
    public PaymentTerms PaymentTerms { get; set; }

    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
}
