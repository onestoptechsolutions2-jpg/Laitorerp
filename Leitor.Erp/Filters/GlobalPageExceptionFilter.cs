using System.Threading.Tasks;
using Leitor.Erp.Localization;
using Leitor.Erp.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Leitor.Erp.Filters;

// Registered as a global MVC filter (see ErpModule.ConfigurePageFilters), so this runs around
// every Razor Page handler in the app without any per-page opt-in. Before this existed, an
// uncaught exception from a standard (non-AJAX) form post - most Create/Edit/Delete/Approve
// handlers throw Volo.Abp.UserFriendlyException for ordinary business-rule failures, see
// Services/**/*AppService.cs - bubbled straight to ABP's generic full-page /Error redirect,
// losing every value the user had typed. This intercepts that, logs anything unexpected, and
// sends the user back to the same page with a friendly toast instead (see
// wwwroot/leitor-notify.js / Components/StatusToast for how the message is actually shown).
//
// This is a safety net, not the final word on data preservation: because the failing POST turns
// into a fresh GET (the standard post/redirect/get pattern), OnGetAsync repopulates the page
// safely, but the specific values the user just typed are not restored. The highest-traffic
// Create/Edit forms get a proper same-request `catch (UserFriendlyException) { ModelState...;
// return Page(); }` in their own OnPostAsync instead (see the UX audit's Phase 3), which this
// filter never even sees because ExceptionHandled is already true by the time it runs.
public class GlobalPageExceptionFilter : IAsyncPageFilter
{
    private readonly ILogger<GlobalPageExceptionFilter> _logger;
    private readonly IStringLocalizer<ErpResource> _localizer;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public GlobalPageExceptionFilter(
        ILogger<GlobalPageExceptionFilter> logger,
        IStringLocalizer<ErpResource> localizer,
        ITempDataDictionaryFactory tempDataFactory)
    {
        _logger = logger;
        _localizer = localizer;
        _tempDataFactory = tempDataFactory;
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var executed = await next();

        if (executed.Exception == null || executed.ExceptionHandled)
        {
            return;
        }

        string message;
        if (executed.Exception is UserFriendlyException friendly)
        {
            // Business-rule failure with an already-well-written message (DependencyGuard,
            // DeletionGate, and ~130 other call sites across Services/**) - not a bug, just log
            // it at a low level for traceability and show it verbatim.
            _logger.LogInformation(executed.Exception, "UserFriendlyException on {Path}", context.HttpContext.Request.Path);
            message = friendly.Message;
        }
        else
        {
            // Genuinely unexpected - log the full exception for developers, never show its
            // details to the user.
            _logger.LogError(executed.Exception, "Unhandled exception on {Path}", context.HttpContext.Request.Path);
            message = _localizer["SomethingWentWrong"];
        }

        var request = context.HttpContext.Request;
        if (OverlayRequest.Is(request))
        {
            executed.Result = new JsonResult(new { error = new { message } }) { StatusCode = 400 };
        }
        else
        {
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            tempData["StatusMessage"] = message;
            tempData["StatusMessageType"] = "error";
            executed.Result = new RedirectResult(request.Path + request.QueryString);
        }

        executed.ExceptionHandled = true;
    }
}
