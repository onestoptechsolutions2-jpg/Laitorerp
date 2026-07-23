using System;
using System.Threading.Tasks;
using Leitor.Erp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace Leitor.Erp.Tests;

// Boots the real Leitor.Erp.Program/ErpModule pipeline behind an in-memory TestServer (not a
// separate test-only module) - ErpModule.OnApplicationInitialization configures the actual
// ASP.NET Core middleware pipeline via context.GetApplicationBuilder(), which only exists under a
// real (or TestServer-backed) web host; the plain non-web ABP test host has no IApplicationBuilder
// at all and throws. ConfigureServices below runs after Program.cs's own service registration, so
// it can swap Postgres for an in-memory Sqlite database and bypass permission checks - the same
// "override services at the WebApplicationFactory layer" pattern ASP.NET Core testing docs
// recommend for any app under test.
public abstract class ErpTestBase : AbpWebApplicationFactoryIntegratedTest<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        base.ConfigureWebHost(builder);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Without this, each ABP unit-of-work opens a real SQLite transaction; Sqlite only allows
        // one writer at a time, and nested/sibling unit-of-work scopes (e.g. IdentityDataSeeder
        // touching several repositories within its own UoW during EnsureDatabaseCreatedAsync)
        // deadlock against each other with "database table is locked".
        services.AddAlwaysDisableUnitOfWorkTransaction();

        // AbpWebApplicationFactoryIntegratedTest's constructor resolves ITestServerAccessor to
        // wire up the TestServer it just built. That registration normally comes from
        // AbpAspNetCoreTestBaseModule, which ErpModule (rightly) doesn't depend on in production -
        // registering it directly here avoids adding a test-only module dependency to prod code.
        services.AddSingleton<ITestServerAccessor, TestServerAccessor>();

        // A single shared SqliteConnection object handed to every DbContext (the usual in-memory
        // Sqlite testing recipe) turns out not to be thread-safe here: ABP resolves several
        // DbContext instances concurrently during module initialization, and their concurrent
        // calls into that one connection's CreateFunction/CreateAggregate registration corrupt a
        // static dictionary inside Microsoft.Data.Sqlite ("concurrent update... corrupted its
        // state"). Using a named shared-cache in-memory database instead gives every DbContext its
        // own SqliteConnection object (safe to initialize concurrently) while they all still read
        // and write the same in-memory data. keepAliveConnection just has to stay open for the
        // process lifetime so SQLite doesn't drop the database once every request-scoped
        // connection closes.
        var connectionString = $"Data Source=file:erp-test-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAliveConnection = new SqliteConnection(connectionString);
        keepAliveConnection.Open();

        services.Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(abpDbContextConfigurationContext =>
            {
                abpDbContextConfigurationContext.DbContextOptions.UseSqlite(connectionString);
            });
        });

        services.AddSingleton(keepAliveConnection);

        services.Replace(ServiceDescriptor.Singleton<IAuthorizationService, AlwaysAllowAuthorizationService>());
    }

    // Each test class instance gets its own fresh in-memory Sqlite connection, so the schema
    // needs (re-)creating per test. EnsureCreatedAsync builds straight from the current EF Core
    // model rather than replaying the Npgsql-only migrations. Data seeding (base currency, tax
    // rates, chart of accounts, etc.) normally only runs via the --migrate-database CLI path (see
    // ErpDbMigrationService) - AppServices like InvoiceAppService that resolve the seeded base
    // currency/tax rates need it run here too, same properties ErpDbMigrationService passes.
    protected async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedAsync(new DataSeedContext(null)
            .WithProperty(IdentityDataSeedContributor.AdminEmailPropertyName, IdentityDataSeedContributor.AdminEmailDefaultValue)
            .WithProperty(IdentityDataSeedContributor.AdminPasswordPropertyName, IdentityDataSeedContributor.AdminPasswordDefaultValue));
    }
}
