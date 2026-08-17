using System;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Calendar;

[Authorize(Policy = ErpPermissions.Calendar.Default)]
public class IndexModel : AbpPageModel
{
    private readonly CalendarEventAppService _calendarEventAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(CalendarEventAppService calendarEventAppService, IFeatureChecker featureChecker)
    {
        _calendarEventAppService = calendarEventAppService;
        _featureChecker = featureChecker;
    }

    public bool CanCreate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Calendar))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Calendar.Create);
        return Page();
    }

    // Pure JSON endpoint FullCalendar's own `events` callback fetches on every view/range change -
    // same "Razor Page handler returning JsonResult" convention as Pages/Search/Index.cshtml.cs,
    // not the auto-generated /api/app/calendar-event/... controller (this app's JS layer never
    // calls those directly, see leitor-layout.js's own comments).
    public async Task<IActionResult> OnGetFeedAsync(DateTime from, DateTime to)
    {
        var feed = await _calendarEventAppService.GetFeedAsync(from, to);
        return new JsonResult(feed);
    }

    public async Task<IActionResult> OnPostMoveAsync(Guid id, DateTime start, DateTime? end)
    {
        await _calendarEventAppService.MoveAsync(id, start, end);
        return new JsonResult(new { ok = true });
    }
}
