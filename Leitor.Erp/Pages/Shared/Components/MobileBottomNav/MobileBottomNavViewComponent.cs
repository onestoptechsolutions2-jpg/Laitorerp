using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Leitor.Erp.Pages.Shared.Components.MobileBottomNav;

// Renders a fixed bottom nav bar on narrow viewports (hidden above the mobile breakpoint in
// leitor-theme.css) - LeptonXLite's own left sidebar remains the primary nav on desktop, but on a
// phone a permanent thumb-reachable strip to the 4 highest-traffic sections beats repeatedly
// opening the hamburger drawer. Same LayoutHooks.Body.Last extension point as
// MyActionItemsViewComponent/GlobalSearchViewComponent (see ErpModule.ConfigureLayoutHooks).
// Unlike AbpPageModel, AbpViewComponent exposes no IsGrantedAsync convenience - IAuthorizationService
// is injected directly and called the plain ASP.NET Core way.
public class MobileBottomNavViewComponent : AbpViewComponent
{
    private readonly IAuthorizationService _authorizationService;

    public MobileBottomNavViewComponent(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var model = new MobileBottomNavViewModel
        {
            CanViewCustomers = (await _authorizationService.AuthorizeAsync(UserClaimsPrincipal, ErpPermissions.Customers.Default)).Succeeded,
            CanViewSales = (await _authorizationService.AuthorizeAsync(UserClaimsPrincipal, ErpPermissions.Sales.Default)).Succeeded,
            CanViewSupport = (await _authorizationService.AuthorizeAsync(UserClaimsPrincipal, ErpPermissions.Support.Default)).Succeeded
        };

        return View(model);
    }
}
