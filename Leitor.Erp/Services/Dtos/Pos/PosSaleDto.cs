using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.Pos;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Pos;

public class PosSaleDto : FullAuditedEntityDto<Guid>
{
    public string SaleNumber { get; set; } = string.Empty;
    public Guid PosSessionId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid SalespersonUserId { get; set; }
    public DateTime SaleDate { get; set; }
    public PosSaleStatus Status { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;
    public string? Notes { get; set; }

    // Resolved/computed by PosSaleAppService - not stored columns.
    public string? CustomerName { get; set; }
    public string? SalespersonUserName { get; set; }
    public List<PosSaleLineDto> Lines { get; set; } = new();
    public List<PosPaymentDto> Payments { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountTendered { get; set; }
}

public class PosSaleLineDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }

    // Computed, never stored - same convention as InvoiceLineDto.LineTotal.
    public decimal LineTotal { get; set; }
}

public class PosPaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public Leitor.Erp.Entities.Sales.PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
}
