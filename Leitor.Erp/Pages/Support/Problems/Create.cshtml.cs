using System;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Support.Problems;

[Authorize(Policy = ErpPermissions.Support.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ProblemAppService _problemAppService;

    public CreateModel(ProblemAppService problemAppService)
    {
        _problemAppService = problemAppService;
    }

    [BindProperty]
    public CreateUpdateProblemDto Problem { get; set; } = new()
    {
        IdentifiedDate = DateTime.Today
    };

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var problem = await _problemAppService.CreateAsync(Problem);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = problem.Id }) });
        }

        return RedirectToPage("./Detail", new { id = problem.Id });
    }
}
