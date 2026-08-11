using Microsoft.AspNetCore.Mvc;

namespace Leitor.Erp.Pages.Shared.Components.FormOverlay;

// Renders the single, page-wide overlay modal shell (empty until wwwroot/leitor-layout.js's
// overlay-form loader fetches a Create/Edit page's markup into it) - same LayoutHooks.Body.Last
// extension point as MyActionItemsViewComponent, see ErpModule.ConfigureLayoutHooks for why that's
// the only supported way to add shell UI on top of the precompiled LeptonXLite layout. No model,
// no per-request state - this is static chrome, unlike MyActionItemsViewComponent.
public class FormOverlayViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
