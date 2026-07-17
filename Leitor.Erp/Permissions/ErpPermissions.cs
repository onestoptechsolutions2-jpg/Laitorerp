namespace Leitor.Erp.Permissions;

public static class ErpPermissions
{
    public static class Customers
    {
        public const string GroupName = "Erp.Customers";
        public const string Default = GroupName;
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
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
}
