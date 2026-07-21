using Leitor.Erp.Localization;
using Volo.Abp.Features;
using Volo.Abp.Localization;
using Volo.Abp.Validation.StringValues;

namespace Leitor.Erp.Features;

// Reuses ABP's own Feature Management module (already referenced in ErpModule.cs/Leitor.Erp.csproj
// but never used for a custom feature until now) as the "turn a module on/off" mechanism, rather
// than inventing a bespoke enabled-modules table - Volo.Abp.FeatureManagement.Web auto-contributes
// its own admin toggle screen the same way Volo.Abp.SettingManagement.Web auto-contributes the
// native Settings menu entry (see Menus/ErpMenuContributor.cs). Every feature here defaults to
// disabled ("false") - these are new, opt-in capabilities layered on top of the always-on core,
// which stays permission-gated only and is never represented here.
public class ErpFeatureDefinitionProvider : FeatureDefinitionProvider
{
    public override void Define(IFeatureDefinitionContext context)
    {
        var group = context.AddGroup(ErpFeatures.GroupName, L("Feature:Erp"));

        group.AddFeature(
            ErpFeatures.ProjectManagement,
            defaultValue: "false",
            displayName: L("Feature:ProjectManagement"),
            valueType: new ToggleStringValueType());

        group.AddFeature(
            ErpFeatures.TaxCompliance,
            defaultValue: "false",
            displayName: L("Feature:TaxCompliance"),
            valueType: new ToggleStringValueType());

        group.AddFeature(
            ErpFeatures.ServiceCatalog,
            defaultValue: "false",
            displayName: L("Feature:ServiceCatalog"),
            valueType: new ToggleStringValueType());

        group.AddFeature(
            ErpFeatures.ServiceRequestManagement,
            defaultValue: "false",
            displayName: L("Feature:ServiceRequestManagement"),
            valueType: new ToggleStringValueType());

        group.AddFeature(
            ErpFeatures.AssetManagement,
            defaultValue: "false",
            displayName: L("Feature:AssetManagement"),
            valueType: new ToggleStringValueType());

        group.AddFeature(
            ErpFeatures.KnowledgeManagement,
            defaultValue: "false",
            displayName: L("Feature:KnowledgeManagement"),
            valueType: new ToggleStringValueType());
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ErpResource>(name);
    }
}
