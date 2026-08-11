namespace Leitor.Erp.Menus;

public class ErpMenus
{
    private const string Prefix = "Erp";
    public const string Home = Prefix + ".Home";

    // CRM - Lead -> Opportunity -> Customer is one pipeline; grouped as one funnel instead of
    // three unrelated-looking top-level entries.
    public const string Crm = Prefix + ".Crm";
    public const string Leads = Crm + ".Leads";
    public const string Opportunities = Crm + ".Opportunities";
    public const string Customers = Crm + ".Customers";
    public const string ContractTemplates = Crm + ".ContractTemplates";

    public const string Sales = Prefix + ".Sales";
    public const string SalesQuotes = Sales + ".Quotes";
    public const string SalesOrders = Sales + ".Orders";
    public const string SalesInvoices = Sales + ".Invoices";

    // Catalog & Inventory - what you sell plus what you have of it, plus the Catalog-owned
    // reference data (tax rates/categories/price lists) that used to be stranded under
    // Administration > Settings, disconnected from the module it configures.
    public const string CatalogInventory = Prefix + ".CatalogInventory";
    public const string Catalog = CatalogInventory + ".Catalog";
    public const string InventoryWarehouses = CatalogInventory + ".Warehouses";
    public const string InventoryStockMovements = CatalogInventory + ".StockMovements";
    public const string CatalogTaxRates = CatalogInventory + ".TaxRates";
    public const string CatalogCategories = CatalogInventory + ".Categories";
    public const string CatalogPriceLists = CatalogInventory + ".PriceLists";

    // Operations - post-sale delivery/execution work.
    public const string Operations = Prefix + ".Operations";
    public const string OperationsFieldServiceJobs = Operations + ".FieldServiceJobs";
    public const string OperationsProjects = Operations + ".Projects";

    // Service Management - the ITIL-style process family: Incident/Problem Management
    // (ex-Support) plus Service Catalog/Requests/Assets/Knowledge, unified under one parent.
    public const string ServiceManagement = Prefix + ".ServiceManagement";
    public const string ServiceManagementTickets = ServiceManagement + ".Tickets";
    public const string ServiceManagementWarrantyClaims = ServiceManagement + ".WarrantyClaims";
    public const string ServiceManagementProblems = ServiceManagement + ".Problems";
    public const string ServiceManagementServiceCatalog = ServiceManagement + ".ServiceCatalog";
    public const string ServiceManagementServiceRequests = ServiceManagement + ".ServiceRequests";
    public const string ServiceManagementAssets = ServiceManagement + ".Assets";
    public const string ServiceManagementChanges = ServiceManagement + ".Changes";
    public const string ServiceManagementKnowledgeBase = ServiceManagement + ".KnowledgeBase";

    public const string Procurement = Prefix + ".Procurement";
    public const string ProcurementVendors = Procurement + ".Vendors";
    public const string ProcurementPurchaseOrders = Procurement + ".PurchaseOrders";
    public const string ProcurementSupplierInvoices = Procurement + ".SupplierInvoices";

    // Accounting - operational ledger pages plus the Accounting-owned reference data
    // (currencies/exchange rates/chart of accounts) that used to be stranded under
    // Administration > Settings, and Tax Compliance (a compliance report, not its own category).
    public const string Accounting = Prefix + ".Accounting";
    public const string AccountingJournalEntries = Accounting + ".JournalEntries";
    public const string FixedAssets = Accounting + ".FixedAssets";
    public const string BankAccounts = Accounting + ".BankAccounts";
    public const string Budgets = Accounting + ".Budgets";
    public const string FiscalPeriods = Accounting + ".FiscalPeriods";
    public const string RecurringJournals = Accounting + ".RecurringJournals";
    public const string AccountingCurrencies = Accounting + ".Currencies";
    public const string AccountingExchangeRates = Accounting + ".ExchangeRates";
    public const string AccountingChartOfAccounts = Accounting + ".ChartOfAccounts";
    public const string AccountingTaxCompliance = Accounting + ".TaxCompliance";

    public const string Pos = Prefix + ".Pos";
    public const string PosRegister = Pos + ".Register";
    public const string PosSales = Pos + ".Sales";

    public const string Partners = Prefix + ".Partners";
    public const string PartnersDirectory = Partners + ".Directory";
    public const string PartnersAgents = Partners + ".Agents";
    public const string PartnersCommissions = Partners + ".Commissions";

    // The "eventually: Cybersecurity" upsell tier - its own top-level area since it's a distinct
    // sellable service line, not folded into Service Management (which is the always-on
    // support/CMDB core).
    public const string Cybersecurity = Prefix + ".Cybersecurity";
    public const string CybersecurityAssessments = Cybersecurity + ".Assessments";

    public const string Portal = Prefix + ".Portal";
    public const string Workspace = Prefix + ".Workspace";
    public const string Help = Prefix + ".Help";
    public const string ModuleToggles = Prefix + ".ModuleToggles";

    // Governance - approval-workflow and workflow-monitoring pages, unified under one parent
    // matching the existing Pages/Governance/ folder both already route under. Previously
    // DeletionApprovals sat bare under Administration and WorkflowMonitor was buried inside the
    // general Reports grab-bag as one item among six - reference-ERP-inspired restructure
    // (2026-08-11) pulled both into their own labeled area, matching Leitor's own
    // approval-workflow accumulation direction (see project_module_toggle_framework memory).
    public const string Governance = Prefix + ".Governance";
    public const string GovernanceDeletionApprovals = Governance + ".DeletionApprovals";
    public const string GovernanceWorkflowMonitor = Governance + ".WorkflowMonitor";

    // Audit Logs - promoted out of the Reports grab-bag to its own top-level Administration area
    // (2026-08-11), matching Pages/AuditLogs already being a physically separate folder from
    // Pages/Governance - a distinct compliance concern, not merged into Governance above.
    public const string AuditLogs = Prefix + ".AuditLogs";

    // Cross-cutting: every read-only analytics/aggregation page that isn't a financial
    // statement, regardless of which business module it reports on - separated from the
    // transactional Module menus above. Financial statements/aging get their own group below
    // since they're 8 of the original 14 items and form one coherent cluster.
    public const string Reports = Prefix + ".Reports";
    public const string ReportsSalesAnalytics = Reports + ".SalesAnalytics";
    public const string ReportsStockOnHand = Reports + ".StockOnHand";
    public const string ReportsLowStock = Reports + ".LowStock";
    public const string ReportsSupportAnalytics = Reports + ".SupportAnalytics";

    public const string FinancialReports = Prefix + ".FinancialReports";
    public const string FinancialReportsTrialBalance = FinancialReports + ".TrialBalance";
    public const string FinancialReportsIncomeStatement = FinancialReports + ".IncomeStatement";
    public const string FinancialReportsBalanceSheet = FinancialReports + ".BalanceSheet";
    public const string FinancialReportsArAging = FinancialReports + ".ArAging";
    public const string FinancialReportsApAging = FinancialReports + ".ApAging";
    public const string FinancialReportsCurrencyRevaluation = FinancialReports + ".CurrencyRevaluation";
    public const string FinancialReportsCashFlow = FinancialReports + ".CashFlow";
    public const string FinancialReportsBudgetVariance = FinancialReports + ".BudgetVariance";

    // Cross-cutting: app-wide config with no single module owner. Module-specific reference
    // data (Catalog's tax rates/categories/price lists, Accounting's currencies/chart of
    // accounts) now lives in those modules' own menus instead of here - see CatalogInventory
    // and Accounting above.
    public const string Settings = Prefix + ".Settings";
    public const string SettingsOperationsConfig = Settings + ".OperationsConfig";

    //Add your menu items here...

}
