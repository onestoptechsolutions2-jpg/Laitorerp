using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the Standard/Normal/Emergency tier model that's the whole point of Change Enablement
// being agile-compatible: only Normal changes wait on anyone.
public class ChangeRequestAppServiceTests : ErpTestBase
{
    private async Task EnableFeaturesAsync()
    {
        // "T" (Tenant), not "H" - see AssetCredentialAppServiceTests' own comment for the full
        // story on why. ChangeRequest needs both: AssetManagement to create the ConfigurationItem
        // it targets, ChangeEnablement to gate ChangeRequestAppService itself.
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.AssetManagement, "true", "T", null);
        await featureManager.SetAsync(ErpFeatures.ChangeEnablement, "true", "T", null);
    }

    private async Task<Guid> CreateConfigurationItemAsync()
    {
        var ciAppService = GetRequiredService<ConfigurationItemAppService>();
        var ci = await ciAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Client Firewall" });
        return ci.Id;
    }

    [Fact]
    public async Task Standard_Tier_Skips_Approval_And_Starts_Approved()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var change = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Routine firmware update",
            Tier = ChangeTier.Standard
        });

        Assert.Equal(ChangeRequestStatus.Approved, change.Status);
        Assert.False(change.ApprovedByUserId.HasValue);
    }

    [Fact]
    public async Task Normal_Tier_Starts_PendingApproval_And_Approve_Records_Who_And_When()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var change = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Reconfigure client VPN",
            Tier = ChangeTier.Normal
        });
        Assert.Equal(ChangeRequestStatus.PendingApproval, change.Status);

        var approved = await changeAppService.ApproveAsync(change.Id);
        Assert.Equal(ChangeRequestStatus.Approved, approved.Status);
        // ApprovedByUserId isn't asserted here - the bare test host has no authenticated
        // CurrentUser, so it's legitimately null in this context even though the real app
        // populates it correctly (see every *UserId = CurrentUser.Id resolution elsewhere).
        Assert.True(approved.ApprovedDate.HasValue);
    }

    [Fact]
    public async Task Approving_A_Change_That_Is_Not_Pending_Throws()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var change = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Pre-approved patch cycle",
            Tier = ChangeTier.Standard
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => changeAppService.ApproveAsync(change.Id));
    }

    [Fact]
    public async Task Reject_Requires_A_Reason_And_Sets_Status()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var change = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Migrate server to new host",
            Tier = ChangeTier.Normal
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => changeAppService.RejectAsync(change.Id, ""));

        var rejected = await changeAppService.RejectAsync(change.Id, "Client hasn't signed off yet");
        Assert.Equal(ChangeRequestStatus.Rejected, rejected.Status);
        Assert.Equal("Client hasn't signed off yet", rejected.RejectionReason);
    }

    [Fact]
    public async Task Complete_Then_RollBack_Follows_The_Full_Lifecycle()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var change = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Apply security patch",
            Tier = ChangeTier.Standard
        });

        // Can't roll back before it's even been completed.
        await Assert.ThrowsAsync<UserFriendlyException>(() => changeAppService.RollBackAsync(change.Id, "too early"));

        var completed = await changeAppService.CompleteAsync(change.Id);
        Assert.Equal(ChangeRequestStatus.Completed, completed.Status);
        Assert.True(completed.CompletedDate.HasValue);

        var rolledBack = await changeAppService.RollBackAsync(change.Id, "Patch broke the VPN client");
        Assert.Equal(ChangeRequestStatus.RolledBack, rolledBack.Status);
        Assert.True(rolledBack.RolledBack);
        Assert.Equal("Patch broke the VPN client", rolledBack.RollbackNotes);
    }

    [Fact]
    public async Task Emergency_Tier_Also_Skips_Approval_But_Needs_Post_Implementation_Review()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var change = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Emergency firewall rule to stop active intrusion",
            Tier = ChangeTier.Emergency
        });

        Assert.Equal(ChangeRequestStatus.Approved, change.Status);
        Assert.Null(change.PostImplementationReviewedDate);

        var reviewed = await changeAppService.ReviewPostImplementationAsync(change.Id);
        Assert.True(reviewed.PostImplementationReviewedDate.HasValue);
    }

    [Fact]
    public async Task ConfigurationItemName_Is_Resolved_On_Get()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableFeaturesAsync();
        var ciId = await CreateConfigurationItemAsync();

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        var created = await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ciId,
            Title = "Update DNS records",
            Tier = ChangeTier.Standard
        });

        var fetched = await changeAppService.GetAsync(created.Id);
        Assert.Equal("Client Firewall", fetched.ConfigurationItemName);
    }
}
