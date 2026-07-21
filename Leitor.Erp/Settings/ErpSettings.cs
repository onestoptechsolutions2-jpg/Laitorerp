namespace Leitor.Erp.Settings;

public static class ErpSettings
{
    public const string GroupName = "Erp";

    public const string SlaHoursUrgent = GroupName + ".Support.SlaHours.Urgent";
    public const string SlaHoursHigh = GroupName + ".Support.SlaHours.High";
    public const string SlaHoursMedium = GroupName + ".Support.SlaHours.Medium";
    public const string SlaHoursLow = GroupName + ".Support.SlaHours.Low";

    public const string ContractExpiryAlertLeadDays = GroupName + ".Contracts.ExpiryAlertLeadDays";
}
