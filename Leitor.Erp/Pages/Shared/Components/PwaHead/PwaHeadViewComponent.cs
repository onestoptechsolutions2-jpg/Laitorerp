using Microsoft.AspNetCore.Mvc;

namespace Leitor.Erp.Pages.Shared.Components.PwaHead;

// Renders the <head> tags a browser needs to consider the app installable (manifest link,
// theme-color, apple-touch-icon) - registered at LayoutHooks.Head.Last, same static-chrome shape
// as ThemeFontsViewComponent. The Account (login) layout is excluded from LayoutHooks the same
// way it's excluded from every other hook in this file, so Login.cshtml carries the identical
// three tags directly (see its own @section styles) - a first-time mobile visitor's very first
// page IS the login screen, so the install prompt has to be eligible there too, not just once
// authenticated.
public class PwaHeadViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
