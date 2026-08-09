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

        // CRM - Lead -> Opportunity -> Customer is one pipeline; grouped as one funnel instead of
        // three separate top-level entries.
        var canViewLeads = await context.IsGrantedAsync(ErpPermissions.Leads.Default);
        var canViewOpportunities = await context.IsGrantedAsync(ErpPermissions.Opportunities.Default);
        var canViewCustomers = await context.IsGrantedAsync(ErpPermissions.Customers.Default);

        if (canViewLeads || canViewOpportunities || canViewCustomers)
        {
            var crmMenu = new ApplicationMenuItem(
                ErpMenus.Crm,
                l["Menu:Crm"],
                icon: "fas fa-address-book",
                order: 2
            );

            if (canViewLeads)
            {
                crmMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.Leads, l["Menu:Leads"], "~/Leads", order: 1)
                );
            }

            if (canViewOpportunities)
            {
                crmMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.Opportunities, l["Menu:Opportunities"], "~/Opportunities", order: 2)
                );
            }

            if (canViewCustomers)
            {
                crmMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.Customers, l["Menu:Customers"], "~/Customers", order: 3)
                );
            }

            context.Menu.Items.Add(crmMenu);
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

        // Catalog & Inventory - what you sell plus what you have of it, plus the Catalog-owned
        // reference data (tax rates/categories/price lists) that used to be stranded under
        // Administration > Settings.
        var canViewCatalog = await context.IsGrantedAsync(ErpPermissions.Catalog.Default);
        var canViewInventory = await context.IsGrantedAsync(ErpPermissions.Inventory.Default);

        if (canViewCatalog || canViewInventory)
        {
            var catalogInventoryMenu = new ApplicationMenuItem(
                ErpMenus.CatalogInventory,
                l["Menu:CatalogInventory"],
                icon: "fas fa-boxes-stacked",
                order: 4
            );

            if (canViewCatalog)
            {
                catalogInventoryMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.Catalog, l["Menu:Catalog"], "~/Catalog", order: 1)
                );
            }

            if (canViewInventory)
            {
                catalogInventoryMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.InventoryWarehouses, l["Menu:Warehouses"], "~/Inventory/Warehouses", order: 2)
                );
                catalogInventoryMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.InventoryStockMovements, l["Menu:StockMovements"], "~/Inventory/StockMovements", order: 3)
                );
            }

            if (canViewCatalog)
            {
                catalogInventoryMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.CatalogTaxRates, l["Menu:TaxRates"], "~/Catalog/TaxRates", order: 4)
                );
                catalogInventoryMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.CatalogCategories, l["Menu:Categories"], "~/Catalog/Categories", order: 5)
                );
                catalogInventoryMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.CatalogPriceLists, l["Menu:PriceLists"], "~/Catalog/PriceLists", order: 6)
                );
            }

            context.Menu.Items.Add(catalogInventoryMenu);
        }

        // Operations - post-sale delivery/execution work (Field Service jobs, Projects).
        var canViewFieldService = await context.IsGrantedAsync(ErpPermissions.FieldService.Default);
        var canViewProjects = await context.IsGrantedAsync(ErpPermissions.Projects.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement);

        if (canViewFieldService || canViewProjects)
        {
            var operationsMenu = new ApplicationMenuItem(
                ErpMenus.Operations,
                l["Menu:Operations"],
                icon: "fas fa-truck-fast",
                order: 5
            );

            if (canViewFieldService)
            {
                operationsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.OperationsFieldServiceJobs, l["Menu:FieldService"], "~/FieldService/Jobs", order: 1)
                );
            }

            if (canViewProjects)
            {
                operationsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.OperationsProjects, l["Menu:Projects"], "~/Projects", order: 2)
                );
            }

            context.Menu.Items.Add(operationsMenu);
        }

        // Service Management - the ITIL-style process family: Incident/Problem Management
        // (ex-Support) plus the toggleable Service Catalog/Requests/Assets/Knowledge modules,
        // unified under one parent instead of five separate top-level entries.
        var canViewSupport = await context.IsGrantedAsync(ErpPermissions.Support.Default);
        var canViewServiceCatalog = await context.IsGrantedAsync(ErpPermissions.ServiceCatalog.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ServiceCatalog);
        var canViewServiceRequests = await context.IsGrantedAsync(ErpPermissions.ServiceRequests.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ServiceRequestManagement);
        var canViewAssets = await context.IsGrantedAsync(ErpPermissions.Assets.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.AssetManagement);
        var canViewKnowledgeBase = await context.IsGrantedAsync(ErpPermissions.KnowledgeBase.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.KnowledgeManagement);
        var canViewChanges = await context.IsGrantedAsync(ErpPermissions.Changes.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.ChangeEnablement);

        if (canViewSupport || canViewServiceCatalog || canViewServiceRequests || canViewAssets || canViewKnowledgeBase || canViewChanges)
        {
            var serviceManagementMenu = new ApplicationMenuItem(
                ErpMenus.ServiceManagement,
                l["Menu:ServiceManagement"],
                icon: "fas fa-headset",
                order: 6
            );

            if (canViewSupport)
            {
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementTickets, l["Menu:Tickets"], "~/Support/Tickets", order: 1)
                );
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementWarrantyClaims, l["Menu:WarrantyClaims"], "~/Support/WarrantyClaims", order: 2)
                );
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementProblems, l["Menu:Problems"], "~/Support/Problems", order: 3)
                );
            }

            if (canViewServiceCatalog)
            {
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementServiceCatalog, l["Menu:ServiceCatalog"], "~/ServiceCatalog", order: 4)
                );
            }

            if (canViewServiceRequests)
            {
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementServiceRequests, l["Menu:ServiceRequests"], "~/ServiceRequests", order: 5)
                );
            }

            if (canViewAssets)
            {
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementAssets, l["Menu:Assets"], "~/Assets", order: 6)
                );
            }

            if (canViewKnowledgeBase)
            {
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementKnowledgeBase, l["Menu:KnowledgeBase"], "~/KnowledgeBase", order: 7)
                );
            }

            if (canViewChanges)
            {
                serviceManagementMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ServiceManagementChanges, l["Menu:Changes"], "~/Changes", order: 8)
                );
            }

            context.Menu.Items.Add(serviceManagementMenu);
        }

        var canViewVendors = await context.IsGrantedAsync(ErpPermissions.Vendors.Default);
        var canViewPurchaseOrders = await context.IsGrantedAsync(ErpPermissions.Procurement.Default);

        if (canViewVendors || canViewPurchaseOrders)
        {
            var procurementMenu = new ApplicationMenuItem(
                ErpMenus.Procurement,
                l["Menu:Procurement"],
                icon: "fas fa-truck-ramp-box",
                order: 7
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

        // Accounting - operational ledger pages plus the Accounting-owned reference data
        // (currencies/exchange rates/chart of accounts) that used to be stranded under
        // Administration > Settings, and Tax Compliance (a compliance report, not its own
        // top-level category). canViewAccounting is reused below for the Financial Reports group.
        var canViewAccounting = await context.IsGrantedAsync(ErpPermissions.Accounting.Default);
        var canViewFixedAssets = await context.IsGrantedAsync(ErpPermissions.FixedAssets.Default);
        var canViewBanking = await context.IsGrantedAsync(ErpPermissions.Banking.Default);
        var canViewBudgets = await context.IsGrantedAsync(ErpPermissions.Budgets.Default);
        var canViewFiscalPeriods = await context.IsGrantedAsync(ErpPermissions.FiscalPeriods.Default);
        var canViewTaxCompliance = await context.IsGrantedAsync(ErpPermissions.TaxCompliance.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.TaxCompliance);

        if (canViewAccounting || canViewFixedAssets || canViewBanking || canViewBudgets || canViewFiscalPeriods || canViewTaxCompliance)
        {
            var accountingMenu = new ApplicationMenuItem(
                ErpMenus.Accounting,
                l["Menu:Accounting"],
                icon: "fas fa-scale-balanced",
                order: 8
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

            if (canViewBanking)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.BankAccounts, l["Menu:BankAccounts"], "~/Accounting/BankAccounts", order: 3)
                );
            }

            if (canViewBudgets)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.Budgets, l["Menu:Budgets"], "~/Accounting/Budgets", order: 4)
                );
            }

            if (canViewFiscalPeriods)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.FiscalPeriods, l["Menu:FiscalPeriods"], "~/Accounting/FiscalPeriods", order: 5)
                );
            }

            if (canViewAccounting)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.RecurringJournals, l["Menu:RecurringJournals"], "~/Accounting/RecurringJournals", order: 6)
                );
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.AccountingCurrencies, l["Menu:Currencies"], "~/Accounting/Currencies", order: 7)
                );
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.AccountingExchangeRates, l["Menu:ExchangeRates"], "~/Accounting/ExchangeRates", order: 8)
                );
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.AccountingChartOfAccounts, l["Menu:ChartOfAccounts"], "~/Accounting/ChartOfAccounts", order: 9)
                );
            }

            if (canViewTaxCompliance)
            {
                accountingMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.AccountingTaxCompliance, l["Menu:TaxCompliance"], "~/Tax/VatReturn", order: 10)
                );
            }

            context.Menu.Items.Add(accountingMenu);
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.Pos.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.PointOfSale))
        {
            var posMenu = new ApplicationMenuItem(
                ErpMenus.Pos,
                l["Menu:Pos"],
                icon: "fas fa-cash-register",
                order: 9
            );
            posMenu.AddItem(new ApplicationMenuItem(ErpMenus.PosRegister, l["Menu:PosRegister"], "~/Pos/Register", order: 1));
            posMenu.AddItem(new ApplicationMenuItem(ErpMenus.PosSales, l["Menu:SalesHistory"], "~/Pos/Sales", order: 2));
            context.Menu.Items.Add(posMenu);
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.Partners.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission))
        {
            var partnersMenu = new ApplicationMenuItem(
                ErpMenus.Partners,
                l["Menu:Partners"],
                icon: "fas fa-handshake",
                order: 10
            );
            partnersMenu.AddItem(new ApplicationMenuItem(ErpMenus.PartnersDirectory, l["Menu:PartnersDirectory"], "~/Partners", order: 1));
            partnersMenu.AddItem(new ApplicationMenuItem(ErpMenus.PartnersAgents, l["Menu:PartnersAgents"], "~/Agents", order: 2));
            partnersMenu.AddItem(new ApplicationMenuItem(ErpMenus.PartnersCommissions, l["Menu:PartnersCommissions"], "~/Commissions", order: 3));
            context.Menu.Items.Add(partnersMenu);
        }

        // Toggleable module (see Features/ErpFeatures.cs).
        if (await context.IsGrantedAsync(ErpPermissions.Cybersecurity.Default) &&
            await featureChecker.IsEnabledAsync(ErpFeatures.Cybersecurity))
        {
            var cybersecurityMenu = new ApplicationMenuItem(
                ErpMenus.Cybersecurity,
                l["Menu:Cybersecurity"],
                icon: "fas fa-shield-halved",
                order: 11
            );
            cybersecurityMenu.AddItem(new ApplicationMenuItem(ErpMenus.CybersecurityAssessments, l["Menu:CybersecurityAssessments"], "~/Cybersecurity/Assessments", order: 1));
            context.Menu.Items.Add(cybersecurityMenu);
        }

        // Cross-cutting: every read-only analytics/aggregation page that isn't a financial
        // statement, regardless of which business module it reports on - lives under
        // Administration (not the main module area) since it's a cross-module utility area, same
        // reasoning AuditLogs/DeletionApprovals already follow. Shown if the user can see any one
        // of its children. Financial statements/aging get their own group below.
        var canViewWorkflowMonitor = await context.IsGrantedAsync(ErpPermissions.Opportunities.Default);
        var canViewSalesAnalytics = await context.IsGrantedAsync(ErpPermissions.Sales.Default);
        var canViewAuditLogs = await context.IsGrantedAsync(ErpPermissions.AuditLogs.Default);
        var canViewInventoryReports = await context.IsGrantedAsync(ErpPermissions.Inventory.Default);
        var canViewSupportAnalytics = await context.IsGrantedAsync(ErpPermissions.Support.Default);

        if (canViewWorkflowMonitor || canViewSalesAnalytics || canViewAuditLogs || canViewInventoryReports || canViewSupportAnalytics)
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

            if (canViewInventoryReports)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsStockOnHand, l["Menu:StockOnHand"], "~/Inventory/Reports/StockOnHand", order: 3)
                );
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsLowStock, l["Menu:LowStock"], "~/Inventory/Reports/LowStock", order: 4)
                );
            }

            if (canViewSupportAnalytics)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsSupportAnalytics, l["Menu:SupportAnalytics"], "~/Support/Analytics", order: 5)
                );
            }

            if (canViewAuditLogs)
            {
                reportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.ReportsAuditLogs, l["Menu:AuditLogs"], "~/AuditLogs", order: 6)
                );
            }

            administration.Items.Add(reportsMenu);
        }

        // Financial statements/aging - split out from Reports above since these 8 are one
        // coherent cluster (all gated on the same Accounting permission) rather than a grab-bag
        // of cross-module analytics.
        if (canViewAccounting)
        {
            var financialReportsMenu = new ApplicationMenuItem(
                ErpMenus.FinancialReports,
                l["Menu:FinancialReports"],
                icon: "fas fa-coins"
            );

            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsTrialBalance, l["Menu:TrialBalance"], "~/Accounting/Reports/TrialBalance", order: 1)
            );
            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsIncomeStatement, l["Menu:IncomeStatement"], "~/Accounting/Reports/IncomeStatement", order: 2)
            );
            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsBalanceSheet, l["Menu:BalanceSheet"], "~/Accounting/Reports/BalanceSheet", order: 3)
            );
            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsArAging, l["Menu:ArAging"], "~/Accounting/Reports/ArAging", order: 4)
            );
            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsApAging, l["Menu:ApAging"], "~/Accounting/Reports/ApAging", order: 5)
            );
            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsCurrencyRevaluation, l["Menu:CurrencyRevaluation"], "~/Accounting/Reports/CurrencyRevaluation", order: 6)
            );
            financialReportsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.FinancialReportsCashFlow, l["Menu:CashFlow"], "~/Accounting/Reports/CashFlow", order: 7)
            );

            if (canViewBudgets)
            {
                financialReportsMenu.AddItem(
                    new ApplicationMenuItem(ErpMenus.FinancialReportsBudgetVariance, l["Menu:BudgetVariance"], "~/Accounting/Reports/BudgetVariance", order: 8)
                );
            }

            administration.Items.Add(financialReportsMenu);
        }

        // Cross-cutting: app-wide config with no single module owner. Module-specific reference
        // data (Catalog's tax rates/categories/price lists, Accounting's currencies/chart of
        // accounts) now lives in those modules' own menus instead of here. Rather than adding a
        // second, competing "Settings" entry, this is appended onto the Settings group the
        // SettingManagement module already contributes to Administration (Email/Timezone etc.) -
        // one Settings menu in the whole app, not two.
        var canManageAppSettings = await context.IsGrantedAsync(ErpPermissions.AppSettings.Manage);

        if (canManageAppSettings)
        {
            var nativeSettingsGroup = administration.Items.FirstOrDefault(x => x.Name == SettingManagementMenuNames.GroupName);

            // Falls back to a standalone group in the (unexpected) case the SettingManagement
            // module hasn't contributed its own item yet when this contributor runs.
            var settingsMenu = nativeSettingsGroup ?? new ApplicationMenuItem(
                ErpMenus.Settings,
                l["Menu:Settings"],
                icon: "fas fa-gears"
            );

            settingsMenu.AddItem(
                new ApplicationMenuItem(ErpMenus.SettingsAppSettings, l["Menu:AppSettings"], "~/Administration/AppSettings", order: 20)
            );

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
