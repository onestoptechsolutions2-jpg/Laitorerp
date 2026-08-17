using Microsoft.AspNetCore.Mvc;

namespace Leitor.Erp.Pages.Shared.Components.GlobalSearch;

// Renders the floating search trigger + its (initially hidden) results panel - one instance per
// page, same LayoutHooks.Body.Last extension point as MyActionItemsViewComponent/
// FormOverlayViewComponent. Fixed-position markup, deliberately not injected into the theme's
// own header - LeptonXLite's header is a precompiled Razor Class Library with no source to
// safely extend (see ErpModule.ConfigureLayoutHooks's own comment), and a fixed-position element
// renders correctly regardless of where in <body> it physically sits, same reasoning already
// proven by the right-hand action panel's own floating edge tab. No model, no per-request state.
public class GlobalSearchViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
