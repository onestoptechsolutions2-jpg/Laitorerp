using Leitor.Erp.Localization;
using Leitor.Erp.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace Leitor.Erp.Menus;

public class ErpMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var administration = context.Menu.GetAdministration();
        var l = context.GetLocalizer<ErpResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                ErpMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );

        if (await context.IsGrantedAsync(ErpPermissions.Customers.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Customers,
                    l["Menu:Customers"],
                    "~/Customers",
                    icon: "fas fa-users",
                    order: 1
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Catalog.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Catalog,
                    l["Menu:Catalog"],
                    "~/Catalog",
                    icon: "fas fa-boxes-stacked",
                    order: 2
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Sales.Default))
        {
            var salesMenu = new ApplicationMenuItem(
                ErpMenus.Sales,
                l["Menu:Sales"],
                icon: "fas fa-file-invoice-dollar",
                order: 3
            );

            salesMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SalesQuotes, l["Menu:Quotes"], "~/Sales/Quotes", order: 1)
            );
            salesMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SalesOrders, l["Menu:Orders"], "~/Sales/Orders", order: 2)
            );
            salesMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SalesInvoices, l["Menu:Invoices"], "~/Sales/Invoices", order: 3)
            );

            context.Menu.Items.Add(salesMenu);
        }

        if (await context.IsGrantedAsync(ErpPermissions.FieldService.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.FieldServiceJobs,
                    l["Menu:FieldService"],
                    "~/FieldService/Jobs",
                    icon: "fas fa-truck-fast",
                    order: 4
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Support.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.SupportTickets,
                    l["Menu:Support"],
                    "~/Support/Tickets",
                    icon: "fas fa-headset",
                    order: 5
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Vendors.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.ProcurementVendors,
                    l["Menu:Vendors"],
                    "~/Procurement/Vendors",
                    icon: "fas fa-truck-ramp-box",
                    order: 6
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.AuditLogs.Default))
        {
            administration.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.AuditLogs,
                    l["Menu:AuditLogs"],
                    "~/AuditLogs",
                    icon: "fas fa-clipboard-list"
                )
            );
        }

        if (ErpModule.IsMultiTenant)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }
    }
}
