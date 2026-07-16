using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

public class CustomerContact : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }

    protected CustomerContact()
    {
    }

    public CustomerContact(Guid id, Guid customerId, string fullName)
        : base(id)
    {
        CustomerId = customerId;
        FullName = fullName;
    }
}
