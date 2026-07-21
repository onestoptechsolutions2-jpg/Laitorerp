using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Dtos.Sales;

namespace Leitor.Erp.Pages.Shared;

// Maps each status enum used across the app to one of the five CSS pill-modifier classes
// defined in wwwroot/leitor-theme.css (.pill-success/.pill-warning/.pill-danger/.pill-info/
// .pill-neutral). One method per enum type so callers get compile-time checking instead of a
// stringly-typed switch copy-pasted into every list/detail page.
public static class StatusPill
{
    private const string Success = "pill-success";
    private const string Warning = "pill-warning";
    private const string Danger = "pill-danger";
    private const string Info = "pill-info";
    private const string Neutral = "pill-neutral";

    public static string For(CustomerStatus status) => status switch
    {
        CustomerStatus.Lead => Info,
        CustomerStatus.Active => Success,
        CustomerStatus.Inactive => Neutral,
        _ => Neutral
    };

    public static string For(LeadStatus status) => status switch
    {
        LeadStatus.New => Info,
        LeadStatus.Contacted => Info,
        LeadStatus.Qualified => Warning,
        LeadStatus.Converted => Success,
        LeadStatus.Lost => Neutral,
        _ => Neutral
    };

    public static string For(OpportunityStatus status) => status switch
    {
        OpportunityStatus.Open => Info,
        OpportunityStatus.Won => Success,
        OpportunityStatus.Lost => Neutral,
        _ => Neutral
    };

    public static string For(ProposalStatus status) => status switch
    {
        ProposalStatus.Draft => Neutral,
        ProposalStatus.Sent => Info,
        ProposalStatus.Accepted => Success,
        ProposalStatus.Rejected => Danger,
        _ => Neutral
    };

    public static string For(QuoteStatus status) => status switch
    {
        QuoteStatus.Draft => Neutral,
        QuoteStatus.Sent => Info,
        QuoteStatus.Accepted => Success,
        QuoteStatus.Rejected => Danger,
        QuoteStatus.Expired => Neutral,
        _ => Neutral
    };

    public static string For(OrderStatus status) => status switch
    {
        OrderStatus.Submitted => Info,
        OrderStatus.Confirmed => Warning,
        OrderStatus.Fulfilled => Success,
        OrderStatus.Cancelled => Neutral,
        _ => Neutral
    };

    public static string For(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => Neutral,
        InvoiceStatus.Issued => Info,
        InvoiceStatus.Cancelled => Neutral,
        _ => Neutral
    };

    public static string For(SupplierInvoiceStatus status) => status switch
    {
        SupplierInvoiceStatus.Draft => Neutral,
        SupplierInvoiceStatus.Issued => Info,
        SupplierInvoiceStatus.Cancelled => Neutral,
        _ => Neutral
    };

    public static string For(InvoicePaymentStatus status) => status switch
    {
        InvoicePaymentStatus.Unpaid => Neutral,
        InvoicePaymentStatus.Overdue => Danger,
        InvoicePaymentStatus.PartiallyPaid => Warning,
        InvoicePaymentStatus.PaidInFull => Success,
        InvoicePaymentStatus.Overpaid => Info,
        _ => Neutral
    };

    public static string For(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => Neutral,
        PurchaseOrderStatus.Sent => Info,
        PurchaseOrderStatus.Confirmed => Warning,
        PurchaseOrderStatus.Received => Success,
        PurchaseOrderStatus.Cancelled => Neutral,
        _ => Neutral
    };

    public static string For(TicketStatus status) => status switch
    {
        TicketStatus.Open => Danger,
        TicketStatus.InProgress => Warning,
        TicketStatus.WaitingOnCustomer => Info,
        TicketStatus.Resolved => Success,
        TicketStatus.Closed => Neutral,
        _ => Neutral
    };

    public static string For(DeletionRequestStatus status) => status switch
    {
        DeletionRequestStatus.Pending => Warning,
        DeletionRequestStatus.Approved => Success,
        DeletionRequestStatus.Rejected => Danger,
        _ => Neutral
    };

    public static string For(WarrantyClaimStatus status) => status switch
    {
        WarrantyClaimStatus.Open => Info,
        WarrantyClaimStatus.Approved => Warning,
        WarrantyClaimStatus.Rejected => Danger,
        WarrantyClaimStatus.Resolved => Success,
        _ => Neutral
    };

    public static string For(FieldServiceJobStatus status) => status switch
    {
        FieldServiceJobStatus.Scheduled => Info,
        FieldServiceJobStatus.InProgress => Warning,
        FieldServiceJobStatus.Completed => Success,
        FieldServiceJobStatus.Incomplete => Danger,
        FieldServiceJobStatus.Cancelled => Neutral,
        _ => Neutral
    };

    public static string For(ProblemStatus status) => status switch
    {
        ProblemStatus.Open => Danger,
        ProblemStatus.Investigating => Warning,
        ProblemStatus.KnownError => Info,
        ProblemStatus.Resolved => Success,
        ProblemStatus.Closed => Neutral,
        _ => Neutral
    };

    public static string For(ProjectStatus status) => status switch
    {
        ProjectStatus.Planned => Info,
        ProjectStatus.Active => Warning,
        ProjectStatus.OnHold => Neutral,
        ProjectStatus.Completed => Success,
        ProjectStatus.Cancelled => Neutral,
        _ => Neutral
    };

    public static string For(ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.Submitted => Info,
        ServiceRequestStatus.Approved => Warning,
        ServiceRequestStatus.InProgress => Warning,
        ServiceRequestStatus.Fulfilled => Success,
        ServiceRequestStatus.Rejected => Danger,
        _ => Neutral
    };

    public static string For(ConfigurationItemStatus status) => status switch
    {
        ConfigurationItemStatus.InUse => Success,
        ConfigurationItemStatus.InStorage => Info,
        ConfigurationItemStatus.UnderMaintenance => Warning,
        ConfigurationItemStatus.Retired => Neutral,
        _ => Neutral
    };
}
