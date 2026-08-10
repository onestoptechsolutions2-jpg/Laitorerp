using System.Threading.Tasks;
using Leitor.Erp.Services.Dtos.Workspace;
using Leitor.Erp.Services.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace Leitor.Erp.Pages.Shared.Components.MyActionItems;

// Renders on every page via LayoutHooks.Body.Last (ErpModule.ConfigureLayoutHooks) - reuses
// MyWorkspaceAppService rather than re-querying, so this rail and the full Pages/Workspace/Index
// page can never drift out of sync on what counts as "mine".
public class MyActionItemsViewComponent : ViewComponent
{
    private readonly MyWorkspaceAppService _myWorkspaceAppService;

    public MyActionItemsViewComponent(MyWorkspaceAppService myWorkspaceAppService)
    {
        _myWorkspaceAppService = myWorkspaceAppService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var workspace = await _myWorkspaceAppService.GetAsync();
        return View(workspace);
    }
}
