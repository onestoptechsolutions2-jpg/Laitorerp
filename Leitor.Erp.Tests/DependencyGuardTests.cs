using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the system-wide "block deletion if dependents exist" policy built 2026-08-11
// (DependencyGuard + guards added across ~20 AppServices) - the mechanism itself, one
// financially-critical case (Account), one business-document case (Customer, the biggest
// behavior change - it used to cascade-delete Orders/Invoices/etc. outright), and one of the
// owned-child cascade bugs found and fixed along the way (ConfigurationItem -> AssetCredential).
public class DependencyGuardTests : ErpTestBase
{
    [Fact]
    public async Task EnsureDeletableAsync_Throws_When_A_Check_Finds_Dependents()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() => DependencyGuard.EnsureDeletableAsync(
            (() => Task.FromResult(2), "Widget")
        ));
    }

    [Fact]
    public async Task EnsureDeletableAsync_Does_Not_Throw_When_No_Dependents()
    {
        await DependencyGuard.EnsureDeletableAsync(
            (() => Task.FromResult(0), "Widget")
        );
    }

    [Fact]
    public async Task CustomerAppService_DeleteAsync_Blocks_When_An_Order_References_It()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var orderRepository = GetRequiredService<IRepository<Order, Guid>>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Zikis" });
        await orderRepository.InsertAsync(new Order(Guid.NewGuid(), customer.Id, "SO-TEST-0001"), autoSave: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() => customerAppService.DeleteAsync(customer.Id));
    }

    [Fact]
    public async Task CustomerAppService_DeleteAsync_Succeeds_When_No_Business_Documents_Reference_It()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Mnyanga" });
        await customerAppService.DeleteAsync(customer.Id);

        await Assert.ThrowsAsync<Volo.Abp.Domain.Entities.EntityNotFoundException<Customer>>(() => customerAppService.GetAsync(customer.Id));
    }

    [Fact]
    public async Task AccountAppService_DeleteAsync_Blocks_When_A_JournalEntryLine_References_It()
    {
        await EnsureDatabaseCreatedAsync();

        var accountAppService = GetRequiredService<AccountAppService>();
        var accountRepository = GetRequiredService<IRepository<Account, Guid>>();
        var journalEntryRepository = GetRequiredService<IRepository<JournalEntry, Guid>>();
        var journalEntryLineRepository = GetRequiredService<IRepository<JournalEntryLine, Guid>>();

        var account = new Account(Guid.NewGuid(), "9999", "Test Account", AccountType.Asset);
        await accountRepository.InsertAsync(account, autoSave: true);

        var entry = new JournalEntry(Guid.NewGuid(), "JE-TEST-0001", DateTime.UtcNow, "Test entry");
        await journalEntryRepository.InsertAsync(entry, autoSave: true);
        await journalEntryLineRepository.InsertAsync(new JournalEntryLine(Guid.NewGuid(), entry.Id, account.Id), autoSave: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() => accountAppService.DeleteAsync(account.Id));
    }

    [Fact]
    public async Task ConfigurationItemAppService_DeleteAsync_Cascades_AssetCredential_Rows()
    {
        await EnsureDatabaseCreatedAsync();

        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.AssetManagement, "true", "T", null);

        var configurationItemAppService = GetRequiredService<ConfigurationItemAppService>();
        var assetCredentialAppService = GetRequiredService<AssetCredentialAppService>();
        var credentialRepository = GetRequiredService<IRepository<AssetCredential, Guid>>();

        var ci = await configurationItemAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Core Switch" });
        var credential = await assetCredentialAppService.CreateAsync(new CreateUpdateAssetCredentialDto
        {
            ConfigurationItemId = ci.Id,
            Label = "OS Admin Login",
            Value = "S3cretPassword!"
        });

        await configurationItemAppService.DeleteAsync(ci.Id);

        var remaining = await credentialRepository.GetListAsync(x => x.Id == credential.Id);
        Assert.Empty(remaining);
    }
}
