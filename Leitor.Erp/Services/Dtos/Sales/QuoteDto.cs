using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class QuoteDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public QuoteStatus Status { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public Guid? ProposalId { get; set; }
    public int Version { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;
    public bool IsLocked { get; set; }
    public Guid? UnlockedByUserId { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? SalespersonUserId { get; set; }
    public Guid? MarginOverrideByUserId { get; set; }
    public DateTime? MarginOverrideAt { get; set; }
    public string? MarginOverrideReason { get; set; }

    // Resolved/computed by QuoteAppService - not stored columns, Mapperly won't map them.
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? ProposalNumber { get; set; }
    public string? SalespersonName { get; set; }
    public string? MarginOverrideByUserName { get; set; }

    // Document-level weighted margin across all lines (revenue net of discount vs snapshotted
    // Cost) - null when there are no lines yet or every line is a 0-revenue giveaway, same
    // "can't compute a meaningful percentage" convention as QuoteLineDto.MarginPercent.
    public decimal? MarginPercent { get; set; }
}
