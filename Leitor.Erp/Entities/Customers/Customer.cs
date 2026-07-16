using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

public class Customer : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;
    public string? Notes { get; set; }

    protected Customer()
    {
    }

    public Customer(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
