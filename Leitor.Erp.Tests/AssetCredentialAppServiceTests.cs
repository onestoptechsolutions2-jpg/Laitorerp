using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the encrypted asset-credential store: values are encrypted at rest, decrypted only via
// the dedicated RevealAsync path, and never carried on the regular list/get DTO.
public class AssetCredentialAppServiceTests : ErpTestBase
{
    private async Task EnableAssetManagementFeatureAsync()
    {
        // "T" (Tenant), not "H" - there is no Host provider in this ABP version. See
        // Pages/Administration/ModuleToggles/Index.cshtml.cs's own comment for the full story;
        // TenantFeatureManagementProvider always resolves against CurrentTenant.Id (null here,
        // both in this single-tenant app and in the test host), so a null key is the right scope.
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.AssetManagement, "true", "T", null);
    }

    [Fact]
    public async Task CreateAsync_Encrypts_Value_At_Rest()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableAssetManagementFeatureAsync();

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

        var stored = await credentialRepository.GetAsync(credential.Id);
        Assert.False(string.IsNullOrEmpty(stored.EncryptedValue));
        Assert.NotEqual("S3cretPassword!", stored.EncryptedValue);
    }

    [Fact]
    public async Task RevealAsync_Returns_Original_Plaintext_Value()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableAssetManagementFeatureAsync();

        var configurationItemAppService = GetRequiredService<ConfigurationItemAppService>();
        var assetCredentialAppService = GetRequiredService<AssetCredentialAppService>();

        var ci = await configurationItemAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Core Router" });
        var credential = await assetCredentialAppService.CreateAsync(new CreateUpdateAssetCredentialDto
        {
            ConfigurationItemId = ci.Id,
            Label = "SNMP Community String",
            CredentialType = AssetCredentialType.Other,
            Value = "public-rw-secret"
        });

        var secret = await assetCredentialAppService.RevealAsync(credential.Id);

        Assert.Equal("public-rw-secret", secret.Value);
    }

    [Fact]
    public async Task GetListAsync_Filters_By_ConfigurationItemId()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableAssetManagementFeatureAsync();

        var configurationItemAppService = GetRequiredService<ConfigurationItemAppService>();
        var assetCredentialAppService = GetRequiredService<AssetCredentialAppService>();

        var ciA = await configurationItemAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Asset A" });
        var ciB = await configurationItemAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Asset B" });

        await assetCredentialAppService.CreateAsync(new CreateUpdateAssetCredentialDto
        {
            ConfigurationItemId = ciA.Id,
            Label = "Admin Login",
            Value = "secret-a"
        });
        await assetCredentialAppService.CreateAsync(new CreateUpdateAssetCredentialDto
        {
            ConfigurationItemId = ciB.Id,
            Label = "Admin Login",
            Value = "secret-b"
        });

        var result = await assetCredentialAppService.GetListAsync(new GetAssetCredentialListInput { ConfigurationItemId = ciA.Id });

        Assert.Single(result.Items);
        Assert.Equal("Admin Login", result.Items[0].Label);
    }
}
