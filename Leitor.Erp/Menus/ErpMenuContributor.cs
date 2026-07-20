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

        // Always visible to any logged-in user, staff and portal accounts alike - it routes to
        // whichever portal (Client/Vendor) the account is linked to, or a "not linked" message for
        // staff who have no portal linkage. No permission gate: the linkage itself is the access
        // control (see Pages/Portal/Index.cshtml.cs).
        context.Menu.Items.Add(
            new ApplicationMenuItem(
                ErpMenus.Portal,
                l["Menu:MyPortal"],
                "~/Portal",
                icon: "fas fa-address-card",
                order: 100
            )
        );

        if (await context.IsGrantedAsync(ErpPermissions.Leads.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Leads,
                    l["Menu:Leads"],
                    "~/Leads",
                    icon: "fas fa-user-plus",
                    order: 1
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Customers.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Customers,
                    l["Menu:Customers"],
                    "~/Customers",
                    icon: "fas fa-users",
                    order: 2
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Opportunities.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Opportunities,
                    l["Menu:Opportunities"],
                    "~/Opportunities",
                    icon: "fas fa-bullseye",
                    order: 3
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
                    order: 4
                )
            );
        }

        if (await context.IsGrantedAsync(ErpPermissions.Sales.Default))
        {
            var salesMenu = new ApplicationMenuItem(
                ErpMenus.Sales,
                l["Menu:Sales"],
                icon: "fas fa-file-invoice-dollar",
                order: 5
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
            salesMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SalesAnalytics, l["Menu:SalesAnalytics"], "~/Sales/Analytics", order: 4)
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
                    order: 6
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
                    order: 7
                )
            );
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.SupportWarrantyClaims,
                    l["Menu:WarrantyClaims"],
                    "~/Support/WarrantyClaims",
                    icon: "fas fa-shield-halved",
                    order: 7
                )
            );
        }

        var canViewVendors = await context.IsGrantedAsync(ErpPermissions.Vendors.Default);
        var canViewPurchaseOrders = await context.IsGrantedAsync(ErpPermissions.Procurement.Default);

        if (canViewVendors || canViewPurchaseOrders)
        {
            var procurementMenu = new ApplicationMenuItem(
                ErpMenus.Procurement,
                l["Menu:Procurement"],
                icon: "fas fa-truck-ramp-box",
                order: 8
            );

            if (canViewVendors)
            {
                procurementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ProcurementVendors, l["Menu:Vendors"], "~/Procurement/Vendors", order: 1)
                );
            }

            if (canViewPurchaseOrders)
            {
                procurementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ProcurementPurchaseOrders, l["Menu:PurchaseOrders"], "~/Procurement/PurchaseOrders", order: 2)
                );
                procurementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ProcurementSupplierInvoices, l["Menu:SupplierInvoices"], "~/Procurement/SupplierInvoices", order: 3)
                );
            }

            context.Menu.Items.Add(procurementMenu);
        }

        if (await context.IsGrantedAsync(ErpPermissions.Accounting.Default))
        {
            var accountingMenu = new ApplicationMenuItem(
                ErpMenus.Accounting,
                l["Menu:Accounting"],
                icon: "fas fa-scale-balanced",
                order: 10
            );

            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingCurrencies, l["Menu:Currencies"], "~/Accounting/Currencies", order: 1)
            );
            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingExchangeRates, l["Menu:ExchangeRates"], "~/Accounting/ExchangeRates", order: 2)
            );
            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingChartOfAccounts, l["Menu:ChartOfAccounts"], "~/Accounting/ChartOfAccounts", order: 3)
            );
            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingJournalEntries, l["Menu:JournalEntries"], "~/Accounting/JournalEntries", order: 4)
            );
            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingTrialBalance, l["Menu:TrialBalance"], "~/Accounting/Reports/TrialBalance", order: 5)
            );
            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingIncomeStatement, l["Menu:IncomeStatement"], "~/Accounting/Reports/IncomeStatement", order: 6)
            );
            accountingMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.AccountingBalanceSheet, l["Menu:BalanceSheet"], "~/Accounting/Reports/BalanceSheet", order: 7)
            );

            context.Menu.Items.Add(accountingMenu);
        }

        if (await context.IsGrantedAsync(ErpPermissions.Opportunities.Default))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.WorkflowMonitor,
                    l["Menu:WorkflowMonitor"],
                    "~/Governance/WorkflowMonitor",
                    icon: "fas fa-diagram-project",
                    order: 9
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

        if (await context.IsGrantedAsync(ErpPermissions.DeletionApprovals.Default))
        {
            administration.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.DeletionApprovals,
                    l["Menu:DeletionApprovals"],
                    "~/Governance/DeletionApprovals",
                    icon: "fas fa-trash-can-arrow-up"
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
