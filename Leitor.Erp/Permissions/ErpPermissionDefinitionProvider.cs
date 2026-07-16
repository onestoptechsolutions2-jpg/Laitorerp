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
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ErpResource>(name);
    }
}
