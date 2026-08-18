namespace Leitor.Erp.Settings;

public static class ErpSettings
{
    public const string GroupName = "Erp";

    public const string SlaHoursUrgent = GroupName + ".Support.SlaHours.Urgent";
    public const string SlaHoursHigh = GroupName + ".Support.SlaHours.High";
    public const string SlaHoursMedium = GroupName + ".Support.SlaHours.Medium";
    public const string SlaHoursLow = GroupName + ".Support.SlaHours.Low";

    public const string ContractExpiryAlertLeadDays = GroupName + ".Contracts.ExpiryAlertLeadDays";

    // How many days before a CustomerTask/ProjectTask's DueDate it starts showing as "due soon"
    // (amber) rather than plain, on the My Workspace reminders list and the Customer/Project
    // Detail task tables - see Pages/Shared/TaskDueStatus.cs.
    public const string TaskDueSoonLeadDays = GroupName + ".Tasks.DueSoonLeadDays";

    // Kenya statutory payroll (Services/PayeCalculator.cs, Services/Hr/PayrollRunAppService.cs).
    // PAYE bands/NSSF tiers are seeded tables (Entities/Hr/PayeTaxBand.cs, NssfTier.cs) since
    // they're genuine multi-row structures; these four are single tunable numbers instead, same
    // reasoning as SalesMarginFloorPercent being a setting rather than a table. SHA (Social Health
    // Insurance Act, replaced NHIF Oct 2024) is a flat percentage of gross pay with a minimum
    // floor, not a genuine band table - modeled as a rate + floor here rather than a
    // ShaContributionBand entity. All four values below are seeded with best-effort current rates
    // and MUST be verified against current KRA/SHA published figures before real payroll use - see
    // the seeded defaults in ErpSettingDefinitionProvider for the "verify" comment on each.
    public const string PayePersonalReliefMonthly = GroupName + ".Payroll.PayePersonalReliefMonthly";
    public const string ShaContributionRatePercent = GroupName + ".Payroll.ShaContributionRatePercent";
    public const string ShaContributionMinimum = GroupName + ".Payroll.ShaContributionMinimum";
    public const string HousingLevyEmployeeRatePercent = GroupName + ".Payroll.HousingLevyEmployeeRatePercent";
    public const string HousingLevyEmployerRatePercent = GroupName + ".Payroll.HousingLevyEmployerRatePercent";

    // The minimum acceptable Quote contribution margin (revenue vs snapshotted line Cost, see
    // QuoteAppService's margin gate) - a Quote can't move Draft -> Sent below this without an
    // explicit, permission-gated, logged override. See Sales.OverrideMarginGate.
    public const string SalesMarginFloorPercent = GroupName + ".Sales.MarginFloorPercent";

    // The deposit percentage OrderAppService.OnOrderConfirmedAsync bills automatically the moment
    // a Milestone-terms order is confirmed - previously hardcoded at 50, now admin-editable so a
    // business that wants a different standard deposit doesn't need a code change.
    public const string SalesDefaultDepositPercent = GroupName + ".Sales.DefaultDepositPercent";

    // Company letterhead info shown on generated PDFs (invoices/quotes/orders/POs/proposals/field
    // service jobs/POS receipts) - previously appsettings-only (Documents/ErpCompanyOptions.cs),
    // meaning changing the company address or phone number needed a code redeploy. See
    // Settings/ErpCompanyProfileProvider.cs for how these get resolved at read time.
    public const string CompanyName = GroupName + ".Company.Name";
    public const string CompanyAddressLine = GroupName + ".Company.AddressLine";
    public const string CompanyCity = GroupName + ".Company.City";
    public const string CompanyState = GroupName + ".Company.State";
    public const string CompanyPostalCode = GroupName + ".Company.PostalCode";
    public const string CompanyCountry = GroupName + ".Company.Country";
    public const string CompanyPhone = GroupName + ".Company.Phone";
    public const string CompanyEmail = GroupName + ".Company.Email";

    // Default signatory name used on generated contract PDFs (Documents/ContractPdfDocument.cs) -
    // see Settings/ErpSettingDefinitionProvider.cs for the seeded default.
    public const string CompanyContractSignatoryName = GroupName + ".Company.ContractSignatoryName";
}
