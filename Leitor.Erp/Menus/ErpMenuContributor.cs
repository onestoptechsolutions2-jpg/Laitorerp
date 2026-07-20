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
                    order: 8
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
                order: 9
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
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.AccountingJournalEntries,
                    l["Menu:Accounting"],
                    "~/Accounting/JournalEntries",
                    icon: "fas fa-scale-balanced",
                    order: 10
                )
            );
        }

        // Cross-cutting: every read-only analytics/aggregation page in the app, regardless of
        // which business module it reports on - shown if the user can see any one of them.
        var canViewWorkflowMonitor = await context.IsGrantedAsync(ErpPermissions.Opportunities.Default);
        var canViewSalesAnalytics = await context.IsGrantedAsync(ErpPermissions.Sales.Default);
        var canViewGeneralLedgerReports = await context.IsGrantedAsync(ErpPermissions.Accounting.Default);
        var canViewAuditLogs = await context.IsGrantedAsync(ErpPermissions.AuditLogs.Default);

        if (canViewWorkflowMonitor || canViewSalesAnalytics || canViewGeneralLedgerReports || canViewAuditLogs)
        {
            var reportsMenu = new ApplicationMenuItem(
                ErpMenus.Reports,
                l["Menu:Reports"],
                icon: "fas fa-chart-line",
                order: 11
            );

            if (canViewWorkflowMonitor)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsWorkflowMonitor, l["Menu:WorkflowMonitor"], "~/Governance/WorkflowMonitor", order: 1)
                );
            }

            if (canViewSalesAnalytics)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsSalesAnalytics, l["Menu:SalesAnalytics"], "~/Sales/Analytics", order: 2)
                );
            }

            if (canViewGeneralLedgerReports)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsTrialBalance, l["Menu:TrialBalance"], "~/Accounting/Reports/TrialBalance", order: 3)
                );
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsIncomeStatement, l["Menu:IncomeStatement"], "~/Accounting/Reports/IncomeStatement", order: 4)
                );
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsBalanceSheet, l["Menu:BalanceSheet"], "~/Accounting/Reports/BalanceSheet", order: 5)
                );
            }

            if (canViewAuditLogs)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsAuditLogs, l["Menu:AuditLogs"], "~/AuditLogs", order: 6)
                );
            }

            context.Menu.Items.Add(reportsMenu);
        }

        // Cross-cutting: every rarely-touched reference/configuration page in the app, regardless
        // of which business module it configures - shown if the user can see any one of them.
        var canViewCatalogSettings = await context.IsGrantedAsync(ErpPermissions.Catalog.Default);

        if (canViewCatalogSettings || canViewGeneralLedgerReports)
        {
            var settingsMenu = new ApplicationMenuItem(
                ErpMenus.Settings,
                l["Menu:Settings"],
                icon: "fas fa-gears",
                order: 12
            );

            if (canViewCatalogSettings)
            {
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsTaxRates, l["Menu:TaxRates"], "~/Catalog/TaxRates", order: 1)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsCategories, l["Menu:Categories"], "~/Catalog/Categories", order: 2)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsPriceLists, l["Menu:PriceLists"], "~/Catalog/PriceLists", order: 3)
                );
            }

            if (canViewGeneralLedgerReports)
            {
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsCurrencies, l["Menu:Currencies"], "~/Accounting/Currencies", order: 4)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsExchangeRates, l["Menu:ExchangeRates"], "~/Accounting/ExchangeRates", order: 5)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsChartOfAccounts, l["Menu:ChartOfAccounts"], "~/Accounting/ChartOfAccounts", order: 6)
                );
            }

            context.Menu.Items.Add(settingsMenu);
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
