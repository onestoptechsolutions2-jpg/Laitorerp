using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Procurement;

public class Vendor : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Notes { get; set; }

    // Prefer deactivating over deleting a Vendor that still has payment/history value but is no
    // longer being ordered from - Vendor has no soft-delete/status concept otherwise, unlike
    // Customer's CustomerStatus (see the UX/error-handling audit's "deactivate over delete" pass).
    // Deleting is still available (gated by DeletionGate/DependencyGuard) for a Vendor with no
    // history at all; this is the safer default for one that does.
    public bool IsActive { get; set; } = true;

    // Defaults new PurchaseOrder/SupplierInvoice's PaymentTerms field at creation - mirrors
    // Customer.DefaultPaymentTerms exactly, reuses the same enum from the Sales side rather than
    // duplicating it for Procurement.
    public PaymentTerms DefaultPaymentTerms { get; set; } = PaymentTerms.Net30;

    // Defaults new PurchaseOrder/SupplierInvoice Create pages' currency field - mirrors
    // Customer.DefaultCurrencyCode.
    public string? DefaultCurrencyCode { get; set; }

    // Links this Vendor to the IdentityUser they log in as on the Vendor Portal (see
    // Pages/Portal/Vendor/Index.cshtml.cs) - one login covers both supplier (Purchase Orders) and
    // subcontracted-technician (Field Service Jobs) access, since both reference Vendor already.
    public Guid? PortalUserId { get; set; }

    // A TaxRate row with TaxType.WithholdingTax (see Entities/Sales/TaxType.cs) - optional; a
    // vendor with none set is never withheld from. VendorPaymentAppService snapshots the
    // resulting amount onto VendorPayment.WithholdingTaxAmount at payment time.
    public Guid? WithholdingTaxRateId { get; set; }

    protected Vendor()
    {
    }

    public Vendor(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
