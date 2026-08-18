using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Leitor.Erp.Pages.Shared.Components.StatusToast;

// Renders #leitor-ux-config: a single hidden element carrying (a) the localized default button
// labels wwwroot/leitor-notify.js uses for every toast/confirm dialog it shows, and (b) whatever
// success/error message PageModelExtensions.SetSuccessMessage/SetErrorMessage stashed into
// TempData on the previous request, so it can be flashed as a toast once here and then discarded
// (TempData's normal read-once semantics apply automatically via ITempDataDictionaryFactory).
// Same LayoutHooks.Body.Last extension point as FormOverlayViewComponent - see
// ErpModule.ConfigureLayoutHooks for why that's the only supported way to add shell UI on top of
// the precompiled LeptonXLite layout.
public class StatusToastViewComponent : ViewComponent
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public StatusToastViewComponent(ITempDataDictionaryFactory tempDataFactory)
    {
        _tempDataFactory = tempDataFactory;
    }

    public IViewComponentResult Invoke()
    {
        var tempData = _tempDataFactory.GetTempData(HttpContext);
        var model = new StatusToastModel
        {
            Message = tempData["StatusMessage"] as string,
            Type = tempData["StatusMessageType"] as string ?? "success"
        };
        return View(model);
    }
}

public class StatusToastModel
{
    public string? Message { get; set; }
    public string Type { get; set; } = "success";
}
