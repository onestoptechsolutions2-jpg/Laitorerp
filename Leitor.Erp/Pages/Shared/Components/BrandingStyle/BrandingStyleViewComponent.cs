using System.Threading.Tasks;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Settings;

namespace Leitor.Erp.Pages.Shared.Components.BrandingStyle;

// Emits the settings-driven logo/favicon overrides (see Settings/ErpSettings.BrandingLogoUrl/
// BrandingFaviconUrl) as a small inline <style>/<link> block. The theme reads the app's logo via
// the --lpx-logo/--lpx-logo-icon CSS custom properties (a background-image, not an <img src> bound
// to IBrandingProvider.LogoUrl - confirmed by inspection, the theme's logo markup is a bare div),
// so this is the actual mechanism, not an IBrandingProvider override. Registered at
// LayoutHooks.Head.Last for the main Application layout (see ErpModule.ConfigureLayoutHooks);
// Login.cshtml invokes this same component directly since the Account layout is excluded from
// LayoutHooks (see that method's own comment).
public class BrandingStyleViewComponent : ViewComponent
{
    private readonly ISettingProvider _settingProvider;

    public BrandingStyleViewComponent(ISettingProvider settingProvider)
    {
        _settingProvider = settingProvider;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new BrandingStyleModel
        {
            LogoUrl = await _settingProvider.GetOrNullAsync(ErpSettings.BrandingLogoUrl),
            FaviconUrl = await _settingProvider.GetOrNullAsync(ErpSettings.BrandingFaviconUrl)
        };
        return View(model);
    }
}

public class BrandingStyleModel
{
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
}
