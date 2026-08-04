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
}
