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
            new SettingDefinition(ErpSettings.SalesMarginFloorPercent, "15", L("Setting:SalesMarginFloorPercent")),
            new SettingDefinition(ErpSettings.TaskDueSoonLeadDays, "3", L("Setting:TaskDueSoonLeadDays")),

            // Kenya statutory payroll defaults - best-effort current figures as of this feature's
            // implementation date (2026-08-17): KES 2,400/month PAYE personal relief (Income Tax
            // Act, stable since 2018); SHA at 2.75% of gross pay with a KES 300/month floor
            // (Social Health Insurance Act, replaced NHIF Oct 2024); Affordable Housing Levy at
            // 1.5% employee + 1.5% employer (Affordable Housing Act 2024). VERIFY ALL FOUR against
            // the current published KRA/SHA figures before running real payroll - these change via
            // Finance Act amendments and this plan's authoring context has a fixed knowledge
            // cutoff that may already be stale. Editable afterward via Administration > Operations,
            // same as every other setting in this file - no code deploy needed to correct one.
            new SettingDefinition(ErpSettings.PayePersonalReliefMonthly, "2400", L("Setting:PayePersonalReliefMonthly")),
            new SettingDefinition(ErpSettings.ShaContributionRatePercent, "2.75", L("Setting:ShaContributionRatePercent")),
            new SettingDefinition(ErpSettings.ShaContributionMinimum, "300", L("Setting:ShaContributionMinimum")),
            new SettingDefinition(ErpSettings.HousingLevyEmployeeRatePercent, "1.5", L("Setting:HousingLevyEmployeeRatePercent")),
            new SettingDefinition(ErpSettings.HousingLevyEmployerRatePercent, "1.5", L("Setting:HousingLevyEmployerRatePercent")),

            // Defaults match the appsettings.json "Company" section's own defaults, so nothing
            // changes for an existing deployment until an admin actually edits these.
            new SettingDefinition(ErpSettings.CompanyName, "Leitor Investment Company Ltd", L("Setting:CompanyName")),
            new SettingDefinition(ErpSettings.CompanyAddressLine, "", L("Setting:CompanyAddressLine")),
            new SettingDefinition(ErpSettings.CompanyCity, "", L("Setting:CompanyCity")),
            new SettingDefinition(ErpSettings.CompanyState, "", L("Setting:CompanyState")),
            new SettingDefinition(ErpSettings.CompanyPostalCode, "", L("Setting:CompanyPostalCode")),
            new SettingDefinition(ErpSettings.CompanyCountry, "", L("Setting:CompanyCountry")),
            new SettingDefinition(ErpSettings.CompanyPhone, "", L("Setting:CompanyPhone")),
            new SettingDefinition(ErpSettings.CompanyEmail, "", L("Setting:CompanyEmail")),

            // Matches the real current signatory named in the Managed Services Agreement sample
            // this feature was built from - a genuine current value, not a placeholder.
            new SettingDefinition(ErpSettings.CompanyContractSignatoryName, "Treazer Ominde Akombe", L("Setting:CompanyContractSignatoryName"))
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ErpResource>(name);
    }
}
