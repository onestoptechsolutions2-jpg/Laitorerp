using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sales;

public class Invoice : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }

    protected Invoice()
    {
    }

    public Invoice(Guid id, Guid customerId, string invoiceNumber)
        : base(id)
    {
        CustomerId = customerId;
        InvoiceNumber = invoiceNumber;
    }
}
