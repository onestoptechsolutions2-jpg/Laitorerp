using Microsoft.EntityFrameworkCore;
using Leitor.Erp.Entities.Customers;
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
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerContact> CustomerContacts { get; set; } = null!;
    public DbSet<CustomerContract> CustomerContracts { get; set; } = null!;
    public DbSet<CustomerNote> CustomerNotes { get; set; } = null!;
    public DbSet<CustomerTask> CustomerTasks { get; set; } = null!;
    public DbSet<CustomerAttachment> CustomerAttachments { get; set; } = null!;

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
    }
}
