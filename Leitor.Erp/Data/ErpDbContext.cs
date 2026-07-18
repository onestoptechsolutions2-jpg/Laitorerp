using Microsoft.EntityFrameworkCore;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Procurement;
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

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerContact> CustomerContacts { get; set; } = null!;
    public DbSet<CustomerContract> CustomerContracts { get; set; } = null!;
    public DbSet<CustomerNote> CustomerNotes { get; set; } = null!;
    public DbSet<CustomerTask> CustomerTasks { get; set; } = null!;
    public DbSet<CustomerAttachment> CustomerAttachments { get; set; } = null!;

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductVendor> ProductVendors { get; set; } = null!;
    public DbSet<Quote> Quotes { get; set; } = null!;
    public DbSet<QuoteLine> QuoteLines { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderLine> OrderLines { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;

    public DbSet<FieldServiceJob> FieldServiceJobs { get; set; } = null!;
    public DbSet<FieldServiceJobNote> FieldServiceJobNotes { get; set; } = null!;
    public DbSet<FieldServiceJobPart> FieldServiceJobParts { get; set; } = null!;

    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = null!;

    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<TicketMessage> TicketMessages { get; set; } = null!;

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

        builder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Sku).HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
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
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.QuoteNumber).IsUnique();
        });

        builder.Entity<QuoteLine>(b =>
        {
            b.ToTable("QuoteLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.QuoteId);
        });

        builder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.ConfigureByConvention();
            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.OrderNumber).IsUnique();
        });

        builder.Entity<OrderLine>(b =>
        {
            b.ToTable("OrderLines");
            b.ConfigureByConvention();
            b.Property(x => x.Description).IsRequired().HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
            b.HasIndex(x => x.OrderId);
        });

        builder.Entity<Invoice>(b =>
        {
            b.ToTable("Invoices");
            b.ConfigureByConvention();
            b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Notes).HasMaxLength(2000);
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
            b.HasIndex(x => x.InvoiceId);
        });

        builder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.Reference).HasMaxLength(128);
            b.Property(x => x.Notes).HasMaxLength(2000);
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
    }
}
