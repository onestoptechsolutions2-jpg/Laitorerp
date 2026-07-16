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
