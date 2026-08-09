using System.Threading.Tasks;
using Volo.Abp.Account.Settings;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;

namespace Leitor.Erp.Data;

// Staff/portal accounts are provisioned by an admin (see ErpRolePermissionDataSeeder, Portal
// linkage) - self-service sign-up has no place on an internal business tool, so this turns off
// ABP's default-on self-registration setting. Runs automatically alongside the other
// IDataSeedContributor implementations (see ErpRolePermissionDataSeeder for how/when).
public class ErpAccountSettingsDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly ISettingManager _settingManager;

    public ErpAccountSettingsDataSeeder(ISettingManager settingManager)
    {
        _settingManager = settingManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await _settingManager.SetGlobalAsync(AccountSettingNames.IsSelfRegistrationEnabled, "false");
    }
}
