using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// File bytes are stored directly in Postgres (bytea) rather than a Docker volume or object
// storage - see the module plan notes for why: it piggybacks on infrastructure (DB migrate +
// backup) already proven working in this deployment, at the cost of not scaling to very large
// attachment volumes. Revisit with object storage if that becomes a real constraint.
public class CustomerAttachment : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();

    protected CustomerAttachment()
    {
    }

    public CustomerAttachment(Guid id, Guid customerId, string fileName, string contentType, byte[] content)
        : base(id)
    {
        CustomerId = customerId;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
    }
}
