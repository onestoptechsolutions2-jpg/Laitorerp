using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Leitor.Erp.Pages.Shared;

// One place to flash a success/error message across a redirect, instead of each PageModel
// re-declaring its own [TempData] SuccessMessage/ErrorMessage pair and hand-rolled <div
// class="alert-..."> markup (the pre-existing pattern in ~10 files - see e.g.
// Pages/Accounting/BankAccounts/Reconcile.cshtml.cs). Components/StatusToast reads these same
// TempData keys on the next page render and fires the toast via wwwroot/leitor-notify.js's
// abp.notify implementation. TempData is cookie-backed, so this survives both a normal
// RedirectToPage and the overlay-modal's window.location.href navigation (see
// Pages/Shared/Components/FormOverlay).
public static class PageModelExtensions
{
    private const string MessageKey = "StatusMessage";
    private const string TypeKey = "StatusMessageType";

    public static void SetSuccessMessage(this PageModel page, string message)
    {
        page.TempData[MessageKey] = message;
        page.TempData[TypeKey] = "success";
    }

    public static void SetErrorMessage(this PageModel page, string message)
    {
        page.TempData[MessageKey] = message;
        page.TempData[TypeKey] = "error";
    }
}
