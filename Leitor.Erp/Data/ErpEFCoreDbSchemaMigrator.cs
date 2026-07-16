using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace Leitor.Erp.Data;

public class ErpEFCoreDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public ErpEFCoreDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the ErpDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<ErpDbContext>()
            .Database
            .MigrateAsync();
    }
}
