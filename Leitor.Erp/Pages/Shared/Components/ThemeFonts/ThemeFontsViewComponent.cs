using Microsoft.AspNetCore.Mvc;

namespace Leitor.Erp.Pages.Shared.Components.ThemeFonts;

// Renders the Google Fonts <link> tags for Inter (the Warm Sunrise design system's typeface) -
// registered at LayoutHooks.Head.Last rather than a CSS @import in leitor-theme.css, because the
// Global style bundle concatenates global-styles.css before leitor-theme.css; an @import there
// would land after other rules in the combined file, which is invalid CSS and browsers silently
// drop it. A real <link> in <head> has no such ordering constraint. No model, no per-request
// state - same static-chrome shape as FormOverlayViewComponent.
public class ThemeFontsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
