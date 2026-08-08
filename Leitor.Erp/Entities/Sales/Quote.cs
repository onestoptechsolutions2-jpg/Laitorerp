using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class Quote : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public int Version { get; set; } = 1;

    // Snapshotted at creation/edit time via CurrencyRateResolver, never recomputed later - same
    // discipline as QuoteLine.TaxRatePercent. ExchangeRateToBase is "1 unit of CurrencyCode equals
    // this many units of the base currency" as of IssueDate.
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    // Locked once it leaves Draft - same lock/single-use-unlock mechanism as Proposal.IsLocked,
    // enforced in QuoteAppService.MapToEntityAsync.
    public bool IsLocked => Status != QuoteStatus.Draft;

    public Guid? UnlockedByUserId { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }

    // Set by ProposalAppService.ConvertToQuoteAsync when this Quote was generated from a
    // technical Proposal - null for quotes created directly, same optional-provenance pattern as
    // Order.QuoteId/PurchaseOrder.SourceOrderId.
    public Guid? ProposalId { get; set; }

    // Optional price list to use for product pricing in this quote.
    // If set, line items fetch prices from this price list; otherwise use standard product prices.
    // Can be null (use standard prices) or set to one of the customer's assigned price lists.
    public Guid? PriceListId { get; set; }

    // Defaulted from the originating Opportunity's AssignedToUserId when created via
    // Proposal->Quote conversion, else CurrentUser.Id - see QuoteAppService.MapToEntityAsync.
    // Purely attributive (commission/reporting), never enforced against who can edit the quote.
    public Guid? SalespersonUserId { get; set; }

    protected Quote()
    {
    }

    public Quote(Guid id, Guid customerId, string quoteNumber, string title)
        : base(id)
    {
        CustomerId = customerId;
        QuoteNumber = quoteNumber;
        Title = title;
    }
}
