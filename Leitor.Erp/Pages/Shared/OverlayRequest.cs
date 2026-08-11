using Microsoft.AspNetCore.Http;

namespace Leitor.Erp.Pages.Shared;

// Detects a request made by wwwroot/leitor-layout.js's overlay-form loader (see
// Pages/Shared/Components/FormOverlay) rather than a real browser navigation - the same
// X-Requested-With convention ASP.NET Core's own tag helpers and most JS libraries use, so this
// doesn't need a bespoke header/query-param scheme. A participating page checks this twice: once
// in its .cshtml to skip the full Layout when true, once in its OnPostAsync to return a small
// JSON redirect signal instead of a real 302 (fetch() can't act on a redirect's Location the way
// a full navigation would).
public static class OverlayRequest
{
    public static bool Is(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
