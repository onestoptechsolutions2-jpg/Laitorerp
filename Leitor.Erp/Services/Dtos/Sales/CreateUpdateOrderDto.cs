using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateOrderDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? QuoteId { get; set; }

    public Guid? ProjectId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Submitted;

    [Required]
    public DateTime OrderDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    [Required]
    [StringLength(8)]
    public string CurrencyCode { get; set; } = string.Empty;

    // Optional - defaults to whichever Warehouse has IsDefault set when left blank (see
    // OrderAppService.MapToEntityAsync).
    public Guid? WarehouseId { get; set; }
}
