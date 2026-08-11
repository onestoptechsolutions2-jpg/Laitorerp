namespace Leitor.Erp.Settings;

public static class ErpSettings
{
    public const string GroupName = "Erp";

    public const string SlaHoursUrgent = GroupName + ".Support.SlaHours.Urgent";
    public const string SlaHoursHigh = GroupName + ".Support.SlaHours.High";
    public const string SlaHoursMedium = GroupName + ".Support.SlaHours.Medium";
    public const string SlaHoursLow = GroupName + ".Support.SlaHours.Low";

    public const string ContractExpiryAlertLeadDays = GroupName + ".Contracts.ExpiryAlertLeadDays";

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
