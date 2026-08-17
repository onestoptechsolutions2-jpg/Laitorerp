using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateCustomerDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    [StringLength(512)]
    public string? AddressLine { get; set; }

    [StringLength(128)]
    public string? City { get; set; }

    [StringLength(128)]
    public string? State { get; set; }

    [StringLength(32)]
    public string? PostalCode { get; set; }

    [StringLength(128)]
    public string? Country { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public Guid? AccountOwnerUserId { get; set; }

    public Guid? PortalUserId { get; set; }

    public PaymentTerms DefaultPaymentTerms { get; set; } = PaymentTerms.Net30;

    // Deliberately NOT [Required] here: the actual enforcement is UI-only (Pages/Customers/
    // Create.cshtml/Edit.cshtml's dropdown has no "None" option, so a <select> always submits a
    // real value - defaults to the first price list alphabetically if untouched). Adding
    // [Required] at the DTO level would apply to every caller of CustomerAppService.CreateAsync/
    // UpdateAsync uniformly, including ~15 existing test files that create customers as
    // unrelated setup (not testing price lists at all) - a disproportionate blast radius for what
    // is fundamentally a UI/workflow requirement ("you must pick one when creating a customer
    // through the app"), not an API-hardening one. ErpPriceListDataSeeder guarantees the
    // dropdown is never empty on a fresh install.
    public Guid? DefaultPriceListId { get; set; }

    public decimal? CreditLimit { get; set; }

    [StringLength(8)]
    public string? DefaultCurrencyCode { get; set; }

    public decimal DiscountPercent { get; set; }
}
