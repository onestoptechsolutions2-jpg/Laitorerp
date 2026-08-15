using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateQuoteDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public Guid? ProposalId { get; set; }

    [Required]
    [StringLength(8)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional price list to use for product pricing in this quote.
    /// If not set, standard product prices are used.
    /// </summary>
    public Guid? PriceListId { get; set; }

    // Not exposed on the Create/Edit forms - purely attributive (commission/reporting), always
    // auto-resolved server-side (see QuoteAppService.MapToEntityAsync). A caller may still pass an
    // explicit value (e.g. a future API integration); the resolver only fills it in when null.
    public Guid? SalespersonUserId { get; set; }

    // Only meaningful when this edit pushes the Quote from Draft into Sent while its computed
    // margin is below ErpSettings.SalesMarginFloorPercent - see QuoteAppService's margin gate.
    // Ignored (never required) on every other status transition or edit.
    [StringLength(1000)]
    public string? MarginOverrideReason { get; set; }
}
