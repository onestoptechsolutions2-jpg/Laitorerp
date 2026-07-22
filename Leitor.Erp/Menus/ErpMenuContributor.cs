using System.Linq;
using Leitor.Erp.Features;
using Leitor.Erp.Localization;
using Leitor.Erp.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Features;
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
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

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

        // A staff member's own "what's on my plate" view (open Tickets/FieldServiceJobs assigned
        // to them, plus a pending-DeletionRequests count if they can act on those) - distinct from
        // the customer/vendor-facing "My Portal" link above, which routes external accounts to
        // Pages/Portal/Client or /Vendor. No permission gate: every logged-in staff account has a
        // workspace, the same way every account has a Dashboard.
        context.Menu.Items.Add(
            new ApplicationMenuItem(
                ErpMenus.Workspace,
                l["Menu:MyWorkspace"],
                "~/Workspace",
                icon: "fas fa-list-check",
                order: 1
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
            var supportMenu = new ApplicationMenuItem(
                ErpMenus.Support,
                l["Menu:Support"],
                icon: "fas fa-headset",
                order: 7
            );

            supportMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SupportTickets, l["Menu:Tickets"], "~/Support/Tickets", order: 1)
            );
            supportMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SupportWarrantyClaims, l["Menu:WarrantyClaims"], "~/Support/WarrantyClaims", order: 2)
            );
            supportMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SupportProblems, l["Menu:Problems"], "~/Support/Problems", order: 3)
            );

            context.Menu.Items.Add(supportMenu);
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

        var canViewAccounting = await context.IsGrantedAsync(ErpPermissions.Accounting.Default);
        var canViewFixedAssets = await context.IsGrantedAsync(ErpPermissions.FixedAssets.Default);

        if (canViewAccounting || canViewFixedAssets)
        {
            var accountingMenu = new ApplicationMenuItem(
                ErpMenus.Accounting,
                l["Menu:Accounting"],
                icon: "fas fa-scale-balanced",
                order: 9
            );

            if (canViewAccounting)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.AccountingJournalEntries, l["Menu:JournalEntries"], "~/Accounting/JournalEntries", order: 1)
                );
            }

            if (canViewFixedAssets)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.FixedAssets, l["Menu:FixedAssets"], "~/Accounting/FixedAssets", order: 2)
                );
            }

            context.Menu.Items.Add(accountingMenu);
        }

        if (await context.IsGrantedAsync(ErpPermissions.Inventory.Default))
        {
            var inventoryMenu = new ApplicationMenuItem(
                ErpMenus.Inventory,
                l["Menu:Inventory"],
                icon: "fas fa-warehouse",
                order: 10
            );

            inventoryMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.InventoryWarehouses, l["Menu:Warehouses"], "~/Inventory/Warehouses", order: 1)
            );
            inventoryMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.InventoryStockMovements, l["Menu:StockMovements"], "~/Inventory/StockMovements", order: 2)
            );

            context.Menu.Items.Add(inventoryMenu);
        }

        // Toggleable module (see Features/ErpFeatures.cs) - gated on both the permission and the
        // feature being switched on, so an Admin who holds Projects.Default still doesn't see this
        // while the module is disabled from Administration > Module Toggles.
        if (await context.IsGrantedAsync(ErpPermissions.Projects.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Projects,
                    l["Menu:Projects"],
                    "~/Projects",
                    icon: "fas fa-diagram-project",
                    order: 11
                )
            );
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.TaxCompliance.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.TaxCompliance))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.TaxCompliance,
                    l["Menu:TaxCompliance"],
                    "~/Tax/VatReturn",
                    icon: "fas fa-receipt",
                    order: 12
                )
            );
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.ServiceCatalog.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ServiceCatalog))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.ServiceCatalog,
                    l["Menu:ServiceCatalog"],
                    "~/ServiceCatalog",
                    icon: "fas fa-list-check",
                    order: 13
                )
            );
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.ServiceRequests.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ServiceRequestManagement))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.ServiceRequests,
                    l["Menu:ServiceRequests"],
                    "~/ServiceRequests",
                    icon: "fas fa-hand-holding-hand",
                    order: 14
                )
            );
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.Assets.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.AssetManagement))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.Assets,
                    l["Menu:Assets"],
                    "~/Assets",
                    icon: "fas fa-server",
                    order: 15
                )
            );
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.KnowledgeBase.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.KnowledgeManagement))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.KnowledgeBase,
                    l["Menu:KnowledgeBase"],
                    "~/KnowledgeBase",
                    icon: "fas fa-book",
                    order: 16
                )
            );
        }

        // Cross-cutting: every read-only analytics/aggregation page in the app, regardless of
        // which business module it reports on - lives under Administration (not the main module
        // area) since it's a cross-module utility area, same reasoning AuditLogs/DeletionApprovals
        // already follow. Shown if the user can see any one of its children.
        var canViewWorkflowMonitor = await context.IsGrantedAsync(ErpPermissions.Opportunities.Default);
        var canViewSalesAnalytics = await context.IsGrantedAsync(ErpPermissions.Sales.Default);
        var canViewGeneralLedgerReports = await context.IsGrantedAsync(ErpPermissions.Accounting.Default);
        var canViewAuditLogs = await context.IsGrantedAsync(ErpPermissions.AuditLogs.Default);
        var canViewInventoryReports = await context.IsGrantedAsync(ErpPermissions.Inventory.Default);
        var canViewSupportAnalytics = await context.IsGrantedAsync(ErpPermissions.Support.Default);

        if (canViewWorkflowMonitor || canViewSalesAnalytics || canViewGeneralLedgerReports || canViewAuditLogs || canViewInventoryReports || canViewSupportAnalytics)
        {
            var reportsMenu = new ApplicationMenuItem(
                ErpMenus.Reports,
                l["Menu:Reports"],
                icon: "fas fa-chart-line"
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

            if (canViewInventoryReports)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsStockOnHand, l["Menu:StockOnHand"], "~/Inventory/Reports/StockOnHand", order: 7)
                );
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsLowStock, l["Menu:LowStock"], "~/Inventory/Reports/LowStock", order: 8)
                );
            }

            if (canViewSupportAnalytics)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsSupportAnalytics, l["Menu:SupportAnalytics"], "~/Support/Analytics", order: 9)
                );
            }

            administration.Items.Add(reportsMenu);
        }

        // Cross-cutting: every rarely-touched reference/configuration page in the app, regardless
        // of which business module it configures. Rather than adding a second, competing
        // "Settings" entry, these are appended onto the Settings group the SettingManagement
        // module already contributes to Administration (Email/Timezone etc.) - one Settings menu
        // in the whole app, not two.
        var canViewCatalogSettings = await context.IsGrantedAsync(ErpPermissions.Catalog.Default);
        var canManageAppSettings = await context.IsGrantedAsync(ErpPermissions.AppSettings.Manage);

        if (canViewCatalogSettings || canViewGeneralLedgerReports || canManageAppSettings)
        {
            var nativeSettingsGroup = administration.Items.FirstOrDefault(x => x.Name == SettingManagementMenuNames.GroupName);

            // Falls back to a standalone group in the (unexpected) case the SettingManagement
            // module hasn't contributed its own item yet when this contributor runs.
            var settingsMenu = nativeSettingsGroup ?? new ApplicationMenuItem(
                ErpMenus.Settings,
                l["Menu:Settings"],
                icon: "fas fa-gears"
            );

            if (canViewCatalogSettings)
            {
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsTaxRates, l["Menu:TaxRates"], "~/Catalog/TaxRates", order: 10)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsCategories, l["Menu:Categories"], "~/Catalog/Categories", order: 11)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsPriceLists, l["Menu:PriceLists"], "~/Catalog/PriceLists", order: 12)
                );
            }

            if (canViewGeneralLedgerReports)
            {
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsCurrencies, l["Menu:Currencies"], "~/Accounting/Currencies", order: 13)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsExchangeRates, l["Menu:ExchangeRates"], "~/Accounting/ExchangeRates", order: 14)
                );
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsChartOfAccounts, l["Menu:ChartOfAccounts"], "~/Accounting/ChartOfAccounts", order: 15)
                );
            }

            if (canManageAppSettings)
            {
                settingsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.SettingsAppSettings, l["Menu:AppSettings"], "~/Administration/AppSettings", order: 20)
                );
            }

            if (nativeSettingsGroup == null)
            {
                administration.Items.Add(settingsMenu);
            }
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

        if (await context.IsGrantedAsync(ErpPermissions.ModuleToggles.Manage))
        {
            administration.Items.Add(
                new ApplicationMenuItem(
                    ErpMenus.ModuleToggles,
                    l["Menu:ModuleToggles"],
                    "~/Administration/ModuleToggles",
                    icon: "fas fa-toggle-on"
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
