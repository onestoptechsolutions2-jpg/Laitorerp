using System;
using Leitor.Erp.Entities.Sales;
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
    public Guid? AccountOwnerUserId { get; set; }

    // Default terms pre-filled onto new Orders/standalone Invoices for this customer - editable
    // per-document afterwards (see OrderAppService.MapToEntityAsync/CopyToEntity).
    public PaymentTerms DefaultPaymentTerms { get; set; } = PaymentTerms.Net30;

    // Suggests a per-customer default price when adding a Quote/Order line for a Product that has
    // an entry in this list (see Pages/Sales/Quotes/Detail.cshtml.cs's ProductOptions building) -
    // the line's UnitPrice stays a plain editable field either way, this only changes the
    // suggested starting number.
    public Guid? DefaultPriceListId { get; set; }

    // Links this Customer to the IdentityUser they log in as on the Client Portal (see
    // Pages/Portal/Client/Index.cshtml.cs). Presence of this link is itself the portal
    // authorization - no separate permission is granted for it.
    public Guid? PortalUserId { get; set; }

    protected Customer()
    {
    }

    public Customer(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
