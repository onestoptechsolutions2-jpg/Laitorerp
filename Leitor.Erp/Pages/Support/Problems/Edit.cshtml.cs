using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Support.Problems;

[Authorize(Policy = ErpPermissions.Support.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ProblemAppService _problemAppService;

    public EditModel(ProblemAppService problemAppService)
    {
        _problemAppService = problemAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateProblemDto Problem { get; set; } = new();

    public async Task OnGetAsync()
    {
        var problem = await _problemAppService.GetAsync(Id);
        Problem = new CreateUpdateProblemDto
        {
            Title = problem.Title,
            Description = problem.Description,
            Status = problem.Status,
            RootCause = problem.RootCause,
            Workaround = problem.Workaround,
            IdentifiedDate = problem.IdentifiedDate
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _problemAppService.UpdateAsync(Id, Problem);
        return RedirectToPage("./Detail", new { id = Id });
    }
}
