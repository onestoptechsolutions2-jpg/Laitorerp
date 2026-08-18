using System.Threading.Tasks;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Partners;
using Leitor.Erp.Services.Procurement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the UX/error-handling audit's "prefer deactivation over delete" pass: Vendor, Partner,
// and Agent previously had no IsActive/status concept at all (hard-delete-only, unlike Employee/
// Product/Warehouse/Account/Currency which already had it) - confirms the new IsActive column
// defaults new records to Active and that the IsActive filter on each GetListAsync actually
// narrows results, the same way GetEmployeeListInput.IsActive already did.
public class DeactivateOverDeleteTests : ErpTestBase
{
    [Fact]
    public async Task VendorAppService_CreateAsync_Defaults_IsActive_True_And_Filter_Narrows_Results()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();

        var active = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Active Supplier" });
        var inactive = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Retired Supplier", IsActive = false });

        Assert.True(active.IsActive);
        Assert.False(inactive.IsActive);

        var activeOnly = await vendorAppService.GetListAsync(new GetVendorListInput { IsActive = true });
        Assert.Contains(activeOnly.Items, x => x.Id == active.Id);
        Assert.DoesNotContain(activeOnly.Items, x => x.Id == inactive.Id);
    }

    [Fact]
    public async Task PartnerAppService_CreateAsync_Defaults_IsActive_True_And_Filter_Narrows_Results()
    {
        await EnsureDatabaseCreatedAsync();
        var partnerAppService = GetRequiredService<PartnerAppService>();

        var active = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var inactive = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Retired Partner", IsActive = false });

        Assert.True(active.IsActive);
        Assert.False(inactive.IsActive);

        var activeOnly = await partnerAppService.GetListAsync(new GetPartnerListInput { IsActive = true });
        Assert.Contains(activeOnly.Items, x => x.Id == active.Id);
        Assert.DoesNotContain(activeOnly.Items, x => x.Id == inactive.Id);
    }

    [Fact]
    public async Task AgentAppService_CreateAsync_Defaults_IsActive_True_And_Filter_Narrows_Results()
    {
        await EnsureDatabaseCreatedAsync();
        var agentAppService = GetRequiredService<AgentAppService>();

        var active = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });
        var inactive = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Retired Agent", IsActive = false });

        Assert.True(active.IsActive);
        Assert.False(inactive.IsActive);

        var activeOnly = await agentAppService.GetListAsync(new GetAgentListInput { IsActive = true });
        Assert.Contains(activeOnly.Items, x => x.Id == active.Id);
        Assert.DoesNotContain(activeOnly.Items, x => x.Id == inactive.Id);
    }
}
