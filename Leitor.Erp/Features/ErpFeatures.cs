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
}
