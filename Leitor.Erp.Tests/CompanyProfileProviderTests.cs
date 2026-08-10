using System.Threading.Tasks;
using Leitor.Erp.Settings;
using Volo.Abp.SettingManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers ErpCompanyProfileProvider - the Settings-backed replacement for the old
// appsettings-only ErpCompanyOptions/IOptions binding, built as part of the Administration >
// Operations & Config hub (2026-08-10) so company letterhead info shown on every generated PDF no
// longer needs a code redeploy to change.
public class CompanyProfileProviderTests : ErpTestBase
{
    [Fact]
    public async Task GetAsync_Returns_Default_Name_When_No_Setting_Saved_Yet()
    {
        await EnsureDatabaseCreatedAsync();

        var provider = GetRequiredService<ErpCompanyProfileProvider>();

        var profile = await provider.GetAsync();

        Assert.Equal("Leitor Investment Company Ltd", profile.Name);
        Assert.True(string.IsNullOrEmpty(profile.Phone));
    }

    [Fact]
    public async Task GetAsync_Reflects_Saved_Settings()
    {
        await EnsureDatabaseCreatedAsync();

        var settingManager = GetRequiredService<ISettingManager>();
        await settingManager.SetGlobalAsync(ErpSettings.CompanyName, "Laitor Investment Company Ltd");
        await settingManager.SetGlobalAsync(ErpSettings.CompanyPhone, "+254712345678");
        await settingManager.SetGlobalAsync(ErpSettings.CompanyCity, "Nairobi");

        var provider = GetRequiredService<ErpCompanyProfileProvider>();
        var profile = await provider.GetAsync();

        Assert.Equal("Laitor Investment Company Ltd", profile.Name);
        Assert.Equal("+254712345678", profile.Phone);
        Assert.Equal("Nairobi", profile.City);
    }
}
