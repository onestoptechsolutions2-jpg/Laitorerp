using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Pos;

public class CreatePosSaleDto
{
    [Required]
    public Guid PosSessionId { get; set; }

    public Guid? CustomerId { get; set; }

    [Required]
    [StringLength(8)]
    public string CurrencyCode { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public List<CreatePosSaleLineDto> Lines { get; set; } = new();
    public List<CreatePosPaymentDto> Payments { get; set; } = new();
}

public class CreatePosSaleLineDto
{
    public Guid? ProductId { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }
}

public class CreatePosPaymentDto
{
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Reference { get; set; }
}
