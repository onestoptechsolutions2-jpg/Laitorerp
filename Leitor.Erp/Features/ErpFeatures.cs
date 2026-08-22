namespace Leitor.Erp.Features;

// Names of the optional, toggleable business modules (see ErpFeatureDefinitionProvider) - each
// one is a genuinely new capability layered on top of the always-on core (Sales/Support/
// Accounting/etc, which are gated by permissions only, never by a feature flag). Same
// GroupName/one-const-per-item shape as ErpPermissions.cs.
public static class ErpFeatures
{
    public const string GroupName = "Erp";
    public const string ProjectManagement = GroupName + ".ProjectManagement";
    public const string TaxCompliance = GroupName + ".TaxCompliance";
    public const string ServiceCatalog = GroupName + ".ServiceCatalog";
    public const string ServiceRequestManagement = GroupName + ".ServiceRequestManagement";
    public const string AssetManagement = GroupName + ".AssetManagement";
    public const string KnowledgeManagement = GroupName + ".KnowledgeManagement";
    public const string PointOfSale = GroupName + ".PointOfSale";
    public const string PartnerCommission = GroupName + ".PartnerCommission";
    public const string Cybersecurity = GroupName + ".Cybersecurity";

    // ITIL Change Enablement - tracks deliberate changes to a ConfigurationItem (patches, config
    // changes, migrations) separately from Tickets (which model something reported as broken).
    // Depends on AssetManagement being meaningful (there's nothing to change without a CI), but
    // kept as its own toggle rather than folded in - a business may want the CMDB without the
    // extra change-approval overhead, same reasoning as ServiceRequestManagement being separate
    // from ServiceCatalog.
    public const string ChangeEnablement = GroupName + ".ChangeEnablement";

    // Shared team calendar: standalone CalendarEvent rows plus a read-only merged view of
    // FieldServiceJob/Ticket/ProjectTask/CustomerTask dates. Toggleable like every other module
    // here for consistency, even though nothing else in the app depends on it being on.
    public const string Calendar = GroupName + ".Calendar";

    // Employee directory, leave management, and Kenya statutory payroll (PAYE/NSSF/SHA/Housing
    // Levy). See Entities/Hr/.
    public const string HumanResources = GroupName + ".HumanResources";

    // Bulk SMS to Leads/Customers via the hosted httpSMS API. See Settings/ErpSettings.cs
    // (BulkSmsApiKey/BulkSmsFromNumber) and Services/Sms/.
    public const string BulkSms = GroupName + ".BulkSms";
}
