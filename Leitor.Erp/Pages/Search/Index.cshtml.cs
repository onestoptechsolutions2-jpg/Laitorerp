using System.Threading.Tasks;
using Leitor.Erp.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Search;

// Pure JSON endpoint, not a page a user navigates to directly - the search box injected via
// GlobalSearchViewComponent (LayoutHooks.Body.Last) fetches this on every page. Results are
// already filtered per-entity-type by permission inside GlobalSearchAppService, but [Authorize]
// here means an unauthenticated request never even runs the queries.
[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly GlobalSearchAppService _globalSearchAppService;

    public IndexModel(GlobalSearchAppService globalSearchAppService)
    {
        _globalSearchAppService = globalSearchAppService;
    }

    public async Task<IActionResult> OnGetAsync(string term)
    {
        var results = await _globalSearchAppService.SearchAsync(term);
        return new JsonResult(results);
    }
}
