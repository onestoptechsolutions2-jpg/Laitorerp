using Microsoft.Extensions.Localization;
using Leitor.Erp.Localization;
using Leitor.Erp.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;
using Volo.Abp.Ui.Branding;

namespace Leitor.Erp;

[Dependency(ReplaceServices = true)]
public class ErpBrandingProvider : DefaultBrandingProvider
{
    private readonly IStringLocalizer<ErpResource> _localizer;
    private readonly ISettingProvider _settingProvider;

    public ErpBrandingProvider(IStringLocalizer<ErpResource> localizer, ISettingProvider settingProvider)
    {
        _localizer = localizer;
        _settingProvider = settingProvider;
    }

    // IBrandingProvider.AppName is a synchronous property (consumed deep inside precompiled theme
    // views with no async-friendly render path available to them), so this blocks on the settings
    // lookup rather than exposing an async alternative - acceptable since ISettingProvider caches
    // resolved values, this isn't a hot path. Falls back to the old hardcoded per-locale string
    // only if Erp.Branding.AppName has never been set (see ErpSettingDefinitionProvider).
    public override string AppName
    {
        get
        {
            var configured = _settingProvider.GetOrNullAsync(ErpSettings.BrandingAppName).GetAwaiter().GetResult();
            return string.IsNullOrWhiteSpace(configured) ? _localizer["AppName"] : configured;
        }
    }
}
