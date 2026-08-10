using Leitor.Erp.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Leitor.Erp.Settings;

// Exposes the handful of business-tunable values that were previously hardcoded constants
// (TicketAppService.DefaultSlaWindow's per-priority hours table, ContractExpiryAlertWorker's
// 30-day lead time) as admin-editable settings - the "Administration configurability" gap from
// the 2026-07-19 360 audit. Document-numbering prefixes are deliberately NOT exposed here: they
// feed DocumentNumbering's count-based scheme, and changing one mid-stream risks a duplicate-key
// collision the numbering logic isn't designed to detect.
public class ErpSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(ErpSettings.SlaHoursUrgent, "4", L("Setting:SlaHoursUrgent")),
            new SettingDefinition(ErpSettings.SlaHoursHigh, "24", L("Setting:SlaHoursHigh")),
            new SettingDefinition(ErpSettings.SlaHoursMedium, "72", L("Setting:SlaHoursMedium")),
            new SettingDefinition(ErpSettings.SlaHoursLow, "168", L("Setting:SlaHoursLow")),
            new SettingDefinition(ErpSettings.ContractExpiryAlertLeadDays, "30", L("Setting:ContractExpiryAlertLeadDays")),

            // Defaults match the appsettings.json "Company" section's own defaults, so nothing
            // changes for an existing deployment until an admin actually edits these.
            new SettingDefinition(ErpSettings.CompanyName, "Leitor Investment Company Ltd", L("Setting:CompanyName")),
            new SettingDefinition(ErpSettings.CompanyAddressLine, "", L("Setting:CompanyAddressLine")),
            new SettingDefinition(ErpSettings.CompanyCity, "", L("Setting:CompanyCity")),
            new SettingDefinition(ErpSettings.CompanyState, "", L("Setting:CompanyState")),
            new SettingDefinition(ErpSettings.CompanyPostalCode, "", L("Setting:CompanyPostalCode")),
            new SettingDefinition(ErpSettings.CompanyCountry, "", L("Setting:CompanyCountry")),
            new SettingDefinition(ErpSettings.CompanyPhone, "", L("Setting:CompanyPhone")),
            new SettingDefinition(ErpSettings.CompanyEmail, "", L("Setting:CompanyEmail"))
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ErpResource>(name);
    }
}
