using Leitor.Erp.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Leitor.Erp.Permissions;

public class ErpPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var customersGroup = context.AddGroup(ErpPermissions.Customers.GroupName, L("Permission:Customers"));

        var customersPermission = customersGroup.AddPermission(ErpPermissions.Customers.Default, L("Permission:Customers"));
        customersPermission.AddChild(ErpPermissions.Customers.Create, L("Permission:Create"));
        customersPermission.AddChild(ErpPermissions.Customers.Edit, L("Permission:Edit"));
        customersPermission.AddChild(ErpPermissions.Customers.Delete, L("Permission:Delete"));

        var catalogGroup = context.AddGroup(ErpPermissions.Catalog.GroupName, L("Permission:Catalog"));
        var catalogPermission = catalogGroup.AddPermission(ErpPermissions.Catalog.Default, L("Permission:Catalog"));
        catalogPermission.AddChild(ErpPermissions.Catalog.Create, L("Permission:Create"));
        catalogPermission.AddChild(ErpPermissions.Catalog.Edit, L("Permission:Edit"));
        catalogPermission.AddChild(ErpPermissions.Catalog.Delete, L("Permission:Delete"));

        var salesGroup = context.AddGroup(ErpPermissions.Sales.GroupName, L("Permission:Sales"));
        var salesPermission = salesGroup.AddPermission(ErpPermissions.Sales.Default, L("Permission:Sales"));
        salesPermission.AddChild(ErpPermissions.Sales.Create, L("Permission:Create"));
        salesPermission.AddChild(ErpPermissions.Sales.Edit, L("Permission:Edit"));
        salesPermission.AddChild(ErpPermissions.Sales.Delete, L("Permission:Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ErpResource>(name);
    }
}
