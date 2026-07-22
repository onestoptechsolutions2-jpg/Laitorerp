using Microsoft.EntityFrameworkCore;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.KnowledgeBase;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.Support;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace Leitor.Erp.Data;

public class ErpDbContext : AbpDbContext<ErpDbContext>
{
    public DbSet<Lead> Leads { get; set; } = null!;
    public DbSet<LeadTouch> LeadTouches { get; set; } = null!;

    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<ExchangeRate> ExchangeRates { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
    public DbSet<JournalEntryLine> JournalEntryLines { get; set; } = null!;
    public DbSet<FixedAsset> FixedAssets { get; set; } = null!;
    public DbSet<DepreciationEntry> DepreciationEntries { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<BankStatementLine> BankStatementLines { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<FiscalPeriod> FiscalPeriods { get; set; } = null!;

    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    public DbSet<DeletionRequest> DeletionRequests { get; set; } = null!;
    public DbSet<WorkflowStageEvent> WorkflowStageEvents { get; set; } = null!;

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerContact> CustomerContacts { get; set; } = null!;
    public DbSet<CustomerContract> CustomerContracts { get; set; } = null!;
    public DbSet<CustomerNote> CustomerNotes { get; set; } = null!;
    public DbSet<CustomerTask> CustomerTasks { get; set; } = null!;
    public DbSet<CustomerAttachment> CustomerAttachments { get; set; } = null!;

    public DbSet<Opportunity> Opportunities { get; set; } = null!;
    public DbSet<NeedsAssessment> NeedsAssessments { get; set; } = null!;
    public DbSet<NeedsAssessmentAttachment> NeedsAssessmentAttachments { get; set; } = null!;
    public DbSet<Proposal> Proposals { get; set; } = null!;

    public DbSet<TaxRate> TaxRates { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductVendor> ProductVendors { get; set; } = null!;
    public DbSet<ProductCategory> ProductCategories { get; set; } = null!;
    public DbSet<ProductBundleItem> ProductBundleItems { get; set; } = null!;
    public DbSet<PriceList> PriceLists { get; set; } = null!;
    public DbSet<PriceListItem> PriceListItems { get; set; } = null!;
    public DbSet<Quote> Quotes { get; set; } = null!;
    public DbSet<QuoteLine> QuoteLines { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderLine> OrderLines { get; set; } = null!;
    public DbSet<OrderPaymentMilestone> OrderPaymentMilestones { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;

    public DbSet<FieldServiceJob> FieldServiceJobs { get; set; } = null!;
    public DbSet<FieldServiceJobNote> FieldServiceJobNotes { get; set; } = null!;
    public DbSet<FieldServiceJobPart> FieldServiceJobParts { get; set; } = null!;

    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = null!;
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = null!;
    public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; } = null!;
    public DbSet<SupplierInvoice> SupplierInvoices { get; set; } = null!;
    public DbSet<SupplierInvoiceLine> SupplierInvoiceLines { get; set; } = null!;
    public DbSet<VendorPayment> VendorPayments { get; set; } = null!;

    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<TicketMessage> TicketMessages { get; set; } = null!;
    public DbSet<WarrantyClaim> WarrantyClaims { get; set; } = null!;
    public DbSet<Problem> Problems { get; set; } = null!;

    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectTask> ProjectTasks { get; set; } = null!;

    public DbSet<ServiceCatalogItem> ServiceCatalogItems { get; set; } = null!;

    public DbSet<ServiceRequest> ServiceRequests { get; set; } = null!;

    public DbSet<ConfigurationItem> ConfigurationItems { get; set; } = null!;
    public DbSet<ConfigurationItemRelationship> ConfigurationItemRelationships { get; set; } = null!;

    public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; } = null!;

    public ErpDbContext(DbContextOptions<ErpDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own entities here */

        builder.Entity<Lead>(b =>
        {
            b.ToTable("Leads");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.CompanyName).HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.NormalizedPhone).HasMaxLength(32);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.ConvertedCustomerId);
            b.HasIndex(x => x.NormalizedPhone);
        });

        builder.Entity<LeadTouch>(b =>
        {
            b.ToTable("LeadTouches");
            b.ConfigureByConvention();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.LeadId);
        });

        builder.Entity<Customer>(b =>
        {
            b.ToTable("Customers");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(32);
            b.Property(x => x.AddressLine).HasMaxLength(512);
            b.Property(x => x.City).HasMaxLength(128);
            b.Property(x => x.State).HasMaxLength(128);
            b.Property(x => x.PostalCode).HasMaxLength(32);
            b.Property(x => x.Country).HasMaxLength(128);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.PortalUserId);
            b.HasIndex(x => x.DefaultPriceListId);
        });

        builder.Entity<CustomerContact>(b =>
        {
            b.ToTable("CustomerContacts");
            b.ConfigureByConvention();
            b.Property(x => x.FullName).IsRequired().HasMaxLength(256);
            b.Property(x => x.JobTitle).HasMaxLength(128);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
        });

        builder.Entity<CustomerContract>(b =>
        {
            b.ToTable("CustomerContracts");
            b.ConfigureByConvention();
            b.Property(x => x.ContractNumber).IsRequired().HasMaxLength(64);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Value).HasColumnType("decimal(18,2)");
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
        });

        builder.Entity<CustomerNote>(b =>
        {
            b.ToTable("CustomerNotes");
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired().HasMaxLength(4000);
            b.HasIndex(x => x.CustomerId);
        });

        builder.Entity<CustomerTask>(b =>
        {
            b.ToTable("CustomerTasks");
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
        });

        builder.Entity<CustomerAttachment>(b =>
        {
            b.ToTable("CustomerAttachments");
            b.ConfigureByConvention();
            b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
            b.HasIndex(x => x.CustomerId);
        });

        builder.Entity<Opportunity>(b =>
        {
            b.ToTable("Opportunities");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.EstimatedValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.LostReason).HasMaxLength(2000);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.LeadId);
        });

        builder.Entity<NeedsAssessment>(b =>
        {
            b.ToTable("NeedsAssessments");
            b.ConfigureByConvention();
            b.Property(x => x.Findings).IsRequired().HasMaxLength(4000);
            b.Property(x => x.Risks).HasMaxLength(4000);
            b.Property(x => x.Recommendations).HasMaxLength(4000);
            b.Property(x => x.CustomerRequirements).HasMaxLength(4000);
            b.HasIndex(x => x.OpportunityId);
        });

        builder.Entity<NeedsAssessmentAttachment>(b =>
        {
            b.ToTable("NeedsAssessmentAttachments");
            b.ConfigureByConvention();
            b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
            b.HasIndex(x => x.NeedsAssessmentId);
        });

        builder.Entity<Proposal>(b =>
        {
            b.ToTable("Proposals");
            b.ConfigureByConvention();
            b.Property(x => x.ProposalNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Summary).HasMaxLength(4000);
            b.Property(x => x.ProposedSolution).HasMaxLength(4000);
            b.Property(x => x.Scope).HasMaxLength(4000);
            b.Property(x => x.Timeline).HasMaxLength(2000);
            b.Property(x => x.Assumptions).HasMaxLength(2000);
            b.Property(x => x.Exclusions).HasMaxLength(2000);
            b.Property(x => x.WarrantyAndSupport).HasMaxLength(2000);
            b.Property(x => x.Terms).HasMaxLength(2000);
            b.HasIndex(x => x.OpportunityId);
            b.HasIndex(x => x.ProposalNumber).IsUnique();
        });

        builder.Entity<TaxRate>(b =>
        {
            b.ToTable("TaxRates");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(64);
            b.Property(x => x.Percent).HasColumnType("decimal(5,2)");
        });

        builder.Entity<Currency>(b =>
        {
            b.ToTable("Currencies");
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(8);
            b.Property(x => x.Name).IsRequired().HasMaxLength(64);
            b.Property(x => x.Symbol).IsRequired().HasMaxLength(8);
            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<ExchangeRate>(b =>
        {
            b.ToTable("ExchangeRates");
            b.ConfigureByConvention();
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8);
            b.Property(x => x.RateToBaseCurrency).HasColumnType("decimal(18,6)");
            b.Property(x => x.Source).IsRequired().HasMaxLength(32);
            b.HasIndex(x => new { x.CurrencyCode, x.RateDate }).IsUnique();
        });

        builder.Entity<Account>(b =>
        {
            b.ToTable("Accounts");
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(16);
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<JournalEntry>(b =>
        {
            b.ToTable("JournalEntries");
            b.ConfigureByConvention();
            b.Property(x => x.EntryNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Description).IsRequired().HasMaxLength(256);
            b.Property(x => x.SourceDocumentType).HasMaxLength(64);
            b.HasIndex(x => x.EntryNumber).IsUnique();
            b.HasIndex(x => new { x.SourceDocumentType, x.SourceDocumentId });
            b.HasIndex(x => x.ReversedEntryId);
        });

        builder.Entity<JournalEntryLine>(b =>
        {
            b.ToTable("JournalEntryLines");
            b.ConfigureByConvention();
            b.Property(x => x.Debit).HasColumnType("decimal(18,2)");
            b.Property(x => x.Credit).HasColumnType("decimal(18,2)");
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8);
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)");
            b.HasIndex(x => x.JournalEntryId);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.ProjectId);
        });

        builder.Entity<FixedAsset>(b =>
        {
            b.ToTable("FixedAssets");
            b.ConfigureByConvention();
            b.Property(x => x.AssetNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.PurchaseCost).HasColumnType("decimal(18,2)");
            b.Property(x => x.SalvageValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.AssetNumber).IsUnique();
        });

        builder.Entity<DepreciationEntry>(b =>
        {
            b.ToTable("DepreciationEntries");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.FixedAssetId);
        });

        builder.Entity<BankAccount>(b =>
        {
            b.ToTable("BankAccounts");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.AccountNumber).HasMaxLength(64);
            b.Property(x => x.BankName).HasMaxLength(128);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8);
            b.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        });

        builder.Entity<BankStatementLine>(b =>
        {
            b.ToTable("BankStatementLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.ReferenceNumber).HasMaxLength(64);
            b.HasIndex(x => x.BankAccountId);
            b.HasIndex(x => x.MatchedJournalEntryLineId);
        });

        builder.Entity<Budget>(b =>
        {
            b.ToTable("Budgets");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.AccountId, x.FiscalYear, x.Month }).IsUnique();
        });

        builder.Entity<FiscalPeriod>(b =>
        {
            b.ToTable("FiscalPeriods");
            b.ConfigureByConvention();
            b.HasIndex(x => new { x.Year, x.Month }).IsUnique();
        });

        builder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Sku).HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            b.Property(x => x.ReorderPoint).HasColumnType("decimal(18,2)");
            b.Property(x => x.ReorderQuantity).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.CategoryId);
        });

        builder.Entity<ProductCategory>(b =>
        {
            b.ToTable("ProductCategories");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });

        builder.Entity<ProductBundleItem>(b =>
        {
            b.ToTable("ProductBundleItems");
            b.ConfigureByConvention();
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.BundleProductId);
            b.HasIndex(x => x.ComponentProductId);
        });

        builder.Entity<PriceList>(b =>
        {
            b.ToTable("PriceLists");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });

        builder.Entity<PriceListItem>(b =>
        {
            b.ToTable("PriceListItems");
            b.ConfigureByConvention();
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.PriceListId);
            b.HasIndex(x => x.ProductId);
        });

        builder.Entity<ProductVendor>(b =>
        {
            b.ToTable("ProductVendors");
            b.ConfigureByConvention();
            b.Property(x => x.VendorSku).HasMaxLength(64);
            b.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.VendorId);
        });

        builder.Entity<Quote>(b =>
        {
            b.ToTable("Quotes");
            b.ConfigureByConvention();
            b.Property(x => x.QuoteNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.QuoteNumber).IsUnique();
            b.HasIndex(x => x.ProposalId);
        });

        builder.Entity<QuoteLine>(b =>
        {
            b.ToTable("QuoteLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxRatePercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.QuoteId);
        });

        builder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.ConfigureByConvention();
            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            // Guid.Empty for pre-Inventory-feature rows only - every Order created going forward
            // always gets a real resolved default-warehouse id from OrderAppService.
            b.Property(x => x.WarehouseId).HasDefaultValue(Guid.Empty);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.HasIndex(x => x.ProjectId);
        });

        builder.Entity<OrderLine>(b =>
        {
            b.ToTable("OrderLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxRatePercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.OrderId);
        });

        builder.Entity<OrderPaymentMilestone>(b =>
        {
            b.ToTable("OrderPaymentMilestones");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(256);
            b.Property(x => x.Percent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.OrderId);
        });

        builder.Entity<Invoice>(b =>
        {
            b.ToTable("Invoices");
            b.ConfigureByConvention();
            b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.InvoiceNumber).IsUnique();
        });

        builder.Entity<InvoiceLine>(b =>
        {
            b.ToTable("InvoiceLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.Property(x => x.TaxRatePercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.InvoiceId);
        });

        builder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.Reference).HasMaxLength(128);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            b.HasIndex(x => x.InvoiceId);
        });

        builder.Entity<FieldServiceJob>(b =>
        {
            b.ToTable("FieldServiceJobs");
            b.ConfigureByConvention();
            b.Property(x => x.SiteAddress).HasMaxLength(512);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.VendorId);
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.ConfigurationItemId);
        });

        builder.Entity<FieldServiceJobNote>(b =>
        {
            b.ToTable("FieldServiceJobNotes");
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired().HasMaxLength(4000);
            b.HasIndex(x => x.JobId);
        });

        builder.Entity<FieldServiceJobPart>(b =>
        {
            b.ToTable("FieldServiceJobParts");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.JobId);
        });

        builder.Entity<Vendor>(b =>
        {
            b.ToTable("Vendors");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(64);
            b.Property(x => x.AddressLine).HasMaxLength(512);
            b.Property(x => x.City).HasMaxLength(128);
            b.Property(x => x.State).HasMaxLength(128);
            b.Property(x => x.PostalCode).HasMaxLength(32);
            b.Property(x => x.Country).HasMaxLength(128);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.PortalUserId);
        });

        builder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("PurchaseOrders");
            b.ConfigureByConvention();
            b.Property(x => x.PONumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            b.HasIndex(x => x.VendorId);
            b.HasIndex(x => x.PONumber).IsUnique();
            b.HasIndex(x => x.SourceOrderId);
        });

        builder.Entity<PurchaseOrderLine>(b =>
        {
            b.ToTable("PurchaseOrderLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.PurchaseOrderId);
        });

        builder.Entity<Warehouse>(b =>
        {
            b.ToTable("Warehouses");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Address).HasMaxLength(512);
        });

        builder.Entity<StockMovement>(b =>
        {
            b.ToTable("StockMovements");
            b.ConfigureByConvention();
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.SourceDocumentType).HasMaxLength(64);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.WarehouseId);
            b.HasIndex(x => new { x.SourceDocumentType, x.SourceDocumentId });
        });

        builder.Entity<GoodsReceipt>(b =>
        {
            b.ToTable("GoodsReceipts");
            b.ConfigureByConvention();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.WarehouseId).HasDefaultValue(Guid.Empty);
            b.HasIndex(x => x.PurchaseOrderId);
        });

        builder.Entity<GoodsReceiptLine>(b =>
        {
            b.ToTable("GoodsReceiptLines");
            b.ConfigureByConvention();
            b.Property(x => x.QuantityReceived).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.GoodsReceiptId);
            b.HasIndex(x => x.PurchaseOrderLineId);
        });

        builder.Entity<SupplierInvoice>(b =>
        {
            b.ToTable("SupplierInvoices");
            b.ConfigureByConvention();
            b.Property(x => x.SupplierInvoiceNumber).IsRequired().HasMaxLength(64);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            b.HasIndex(x => x.PurchaseOrderId);
            b.HasIndex(x => x.VendorId);
        });

        builder.Entity<SupplierInvoiceLine>(b =>
        {
            b.ToTable("SupplierInvoiceLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.SupplierInvoiceId);
        });

        builder.Entity<VendorPayment>(b =>
        {
            b.ToTable("VendorPayments");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.WithholdingTaxAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.Reference).HasMaxLength(128);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(8).HasDefaultValue("KES");
            b.Property(x => x.ExchangeRateToBase).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
            b.HasIndex(x => x.SupplierInvoiceId);
        });

        builder.Entity<Ticket>(b =>
        {
            b.ToTable("Tickets");
            b.ConfigureByConvention();
            b.Property(x => x.TicketNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Subject).IsRequired().HasMaxLength(256);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.JobId);
            b.HasIndex(x => x.ContractId);
            b.HasIndex(x => x.ProblemId);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.TicketNumber).IsUnique();
        });

        builder.Entity<TicketMessage>(b =>
        {
            b.ToTable("TicketMessages");
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired().HasMaxLength(4000);
            b.HasIndex(x => x.TicketId);
        });

        builder.Entity<WarrantyClaim>(b =>
        {
            b.ToTable("WarrantyClaims");
            b.ConfigureByConvention();
            b.Property(x => x.ClaimNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.ContractId);
            b.HasIndex(x => x.JobId);
            b.HasIndex(x => x.TicketId);
            b.HasIndex(x => x.ClaimNumber).IsUnique();
        });

        builder.Entity<Problem>(b =>
        {
            b.ToTable("Problems");
            b.ConfigureByConvention();
            b.Property(x => x.ProblemNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.RootCause).HasMaxLength(2000);
            b.Property(x => x.Workaround).HasMaxLength(2000);
            b.HasIndex(x => x.ProblemNumber).IsUnique();
        });

        builder.Entity<Project>(b =>
        {
            b.ToTable("Projects");
            b.ConfigureByConvention();
            b.Property(x => x.ProjectNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Budget).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.ProjectNumber).IsUnique();
        });

        builder.Entity<ProjectTask>(b =>
        {
            b.ToTable("ProjectTasks");
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasIndex(x => x.ProjectId);
        });

        builder.Entity<ServiceCatalogItem>(b =>
        {
            b.ToTable("ServiceCatalogItems");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Category).HasMaxLength(128);
        });

        builder.Entity<ServiceRequest>(b =>
        {
            b.ToTable("ServiceRequests");
            b.ConfigureByConvention();
            b.Property(x => x.RequestNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.ServiceCatalogItemId);
            b.HasIndex(x => x.RequestNumber).IsUnique();
        });

        builder.Entity<ConfigurationItem>(b =>
        {
            b.ToTable("ConfigurationItems");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.SerialNumber).HasMaxLength(128);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
        });

        builder.Entity<ConfigurationItemRelationship>(b =>
        {
            b.ToTable("ConfigurationItemRelationships");
            b.ConfigureByConvention();
            b.HasIndex(x => x.SourceCiId);
            b.HasIndex(x => x.TargetCiId);
        });

        builder.Entity<KnowledgeArticle>(b =>
        {
            b.ToTable("KnowledgeArticles");
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Tags).HasMaxLength(512);
            b.HasIndex(x => x.SourceTicketId);
        });

        builder.Entity<DeletionRequest>(b =>
        {
            b.ToTable("DeletionRequests");
            b.ConfigureByConvention();
            b.Property(x => x.EntityType).IsRequired().HasMaxLength(64);
            b.Property(x => x.Reason).HasMaxLength(2000);
            b.Property(x => x.DecisionNotes).HasMaxLength(2000);
            b.HasIndex(x => new { x.EntityType, x.EntityId });
            b.HasIndex(x => x.Status);
        });

        builder.Entity<WorkflowStageEvent>(b =>
        {
            b.ToTable("WorkflowStageEvents");
            b.ConfigureByConvention();
            b.Property(x => x.EntityType).IsRequired().HasMaxLength(64);
            b.Property(x => x.Channel).HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }
}
