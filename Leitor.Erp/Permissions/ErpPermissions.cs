namespace Leitor.Erp.Permissions;

public static class ErpPermissions
{
    public static class Leads
    {
        public const string GroupName = "Erp.Leads";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Customers
    {
        public const string GroupName = "Erp.Customers";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Covers Opportunity, NeedsAssessment/NeedsAssessmentAttachment, and Proposal together - same
    // convention as Sales covering Quote+Order+Invoice+Payment on separate pages.
    public static class Opportunities
    {
        public const string GroupName = "Erp.Opportunities";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";

        // Separate from Edit: unlocking an approved Proposal for revision is a senior/manager
        // action, not a routine edit - see Services/Opportunities/ProposalAppService.cs.
        public const string Unlock = Default + ".Unlock";
    }

    public static class Catalog
    {
        public const string GroupName = "Erp.Catalog";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Sales
    {
        public const string GroupName = "Erp.Sales";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";

        // Unlocking an approved Quote/Order for revision is a senior/manager action - see
        // Services/Sales/QuoteAppService.cs / OrderAppService.cs.
        public const string Unlock = Default + ".Unlock";
    }

    public static class FieldService
    {
        public const string GroupName = "Erp.FieldService";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Support
    {
        public const string GroupName = "Erp.Support";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Read-only viewer over ABP's audit logging module (already recording every request/entity
    // change - see Volo.Abp.AuditLogging.EntityFrameworkCore in the csproj), so there's only a
    // single view permission - no Create/Edit/Delete, audit logs are never authored or modified.
    public static class AuditLogs
    {
        public const string GroupName = "Erp.AuditLogs";
        public const string Default = GroupName;
    }

    // Vendor master data is split from Procurement the same way Catalog is split from Sales - a
    // vendor directory usable independent of the purchase-order workflow that references it.
    public static class Vendors
    {
        public const string GroupName = "Erp.Vendors";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Procurement
    {
        public const string GroupName = "Erp.Procurement";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Covers Currencies/Exchange Rates, Chart of Accounts, Journal Entries, and the financial
    // reports together - same "one group per nav section" convention as Procurement.
    public static class Accounting
    {
        public const string GroupName = "Erp.Accounting";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Covers Warehouses, Stock Movements, and the Stock on Hand/Low Stock reports - its own group
    // rather than folded into Catalog, since inventory is its own nav section with its own
    // read/write shape (most stock movements are system-generated, not user-authored CRUD).
    public static class Inventory
    {
        public const string GroupName = "Erp.Inventory";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Gated behind ErpFeatures.ProjectManagement (see Features/ErpFeatures.cs) - these permissions
    // only matter while the module is switched on.
    public static class Projects
    {
        public const string GroupName = "Erp.Projects";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Gated behind ErpFeatures.TaxCompliance (see Features/ErpFeatures.cs) - a single view
    // permission, since the VAT return is a read-only report over data already captured
    // elsewhere (Sales/Procurement lines, Vendor withholding rates).
    public static class TaxCompliance
    {
        public const string GroupName = "Erp.TaxCompliance";
        public const string Default = GroupName;
    }

    // Gated behind ErpFeatures.ServiceCatalog (see Features/ErpFeatures.cs).
    public static class ServiceCatalog
    {
        public const string GroupName = "Erp.ServiceCatalog";
        public const string Default = GroupName;
        public const string Edit = Default + ".Edit";
    }

    // Gated behind ErpFeatures.ServiceRequestManagement (see Features/ErpFeatures.cs).
    public static class ServiceRequests
    {
        public const string GroupName = "Erp.ServiceRequests";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Gated behind ErpFeatures.AssetManagement (see Features/ErpFeatures.cs).
    public static class Assets
    {
        public const string GroupName = "Erp.Assets";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Gated behind ErpFeatures.KnowledgeManagement (see Features/ErpFeatures.cs).
    public static class KnowledgeBase
    {
        public const string GroupName = "Erp.KnowledgeBase";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Deletion is permission-based but gated by approval: Decide (Admin/Ops Manager) can delete a
    // scoped entity immediately; everyone else's delete action files a request here instead - see
    // Services/Governance/DeletionGate.cs.
    public static class DeletionApprovals
    {
        public const string GroupName = "Erp.DeletionApprovals";
        public const string Default = GroupName;
        public const string Decide = Default + ".Decide";
    }

    // Gates the admin screen that turns the optional modules in Features/ErpFeatures.cs on/off -
    // a distinct capability from any single module's own permissions, since toggling Project
    // Management off shouldn't require holding Projects.Edit (which won't even exist while it's
    // off) and enabling it is itself a decision worth its own gate.
    public static class ModuleToggles
    {
        public const string GroupName = "Erp.ModuleToggles";
        public const string Default = GroupName;
        public const string Manage = Default + ".Manage";
    }
}
