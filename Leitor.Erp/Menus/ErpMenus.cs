namespace Leitor.Erp.Menus;

public class ErpMenus
{
    private const string Prefix = "Erp";
    public const string Home = Prefix + ".Home";
    public const string Leads = Prefix + ".Leads";
    public const string Customers = Prefix + ".Customers";
    public const string Opportunities = Prefix + ".Opportunities";
    public const string Catalog = Prefix + ".Catalog";
    public const string Sales = Prefix + ".Sales";
    public const string SalesQuotes = Sales + ".Quotes";
    public const string SalesOrders = Sales + ".Orders";
    public const string SalesInvoices = Sales + ".Invoices";
    public const string FieldService = Prefix + ".FieldService";
    public const string FieldServiceJobs = FieldService + ".Jobs";
    public const string Support = Prefix + ".Support";
    public const string SupportTickets = Support + ".Tickets";
    public const string SupportWarrantyClaims = Support + ".WarrantyClaims";
    public const string SupportProblems = Support + ".Problems";
    public const string Procurement = Prefix + ".Procurement";
    public const string ProcurementVendors = Procurement + ".Vendors";
    public const string ProcurementPurchaseOrders = Procurement + ".PurchaseOrders";
    public const string ProcurementSupplierInvoices = Procurement + ".SupplierInvoices";
    public const string Accounting = Prefix + ".Accounting";
    public const string AccountingJournalEntries = Accounting + ".JournalEntries";
    public const string FixedAssets = Accounting + ".FixedAssets";
    public const string BankAccounts = Accounting + ".BankAccounts";
    public const string Inventory = Prefix + ".Inventory";
    public const string InventoryWarehouses = Inventory + ".Warehouses";
    public const string InventoryStockMovements = Inventory + ".StockMovements";
    public const string Projects = Prefix + ".Projects";
    public const string TaxCompliance = Prefix + ".TaxCompliance";
    public const string ServiceCatalog = Prefix + ".ServiceCatalog";
    public const string ServiceRequests = Prefix + ".ServiceRequests";
    public const string Assets = Prefix + ".Assets";
    public const string KnowledgeBase = Prefix + ".KnowledgeBase";
    public const string Portal = Prefix + ".Portal";
    public const string Workspace = Prefix + ".Workspace";
    public const string DeletionApprovals = Prefix + ".DeletionApprovals";
    public const string ModuleToggles = Prefix + ".ModuleToggles";

    // Cross-cutting: every read-only analytics/aggregation page in the app, regardless of which
    // business module it reports on - separated from the transactional Module menus above.
    public const string Reports = Prefix + ".Reports";
    public const string ReportsWorkflowMonitor = Reports + ".WorkflowMonitor";
    public const string ReportsSalesAnalytics = Reports + ".SalesAnalytics";
    public const string ReportsTrialBalance = Reports + ".TrialBalance";
    public const string ReportsIncomeStatement = Reports + ".IncomeStatement";
    public const string ReportsBalanceSheet = Reports + ".BalanceSheet";
    public const string ReportsAuditLogs = Reports + ".AuditLogs";
    public const string ReportsStockOnHand = Reports + ".StockOnHand";
    public const string ReportsLowStock = Reports + ".LowStock";
    public const string ReportsSupportAnalytics = Reports + ".SupportAnalytics";

    // Cross-cutting: every rarely-touched reference/configuration page in the app, regardless of
    // which business module it configures - separated from the transactional Module menus above.
    public const string Settings = Prefix + ".Settings";
    public const string SettingsTaxRates = Settings + ".TaxRates";
    public const string SettingsCategories = Settings + ".Categories";
    public const string SettingsPriceLists = Settings + ".PriceLists";
    public const string SettingsCurrencies = Settings + ".Currencies";
    public const string SettingsExchangeRates = Settings + ".ExchangeRates";
    public const string SettingsChartOfAccounts = Settings + ".ChartOfAccounts";
    public const string SettingsAppSettings = Settings + ".AppSettings";

    //Add your menu items here...

}
