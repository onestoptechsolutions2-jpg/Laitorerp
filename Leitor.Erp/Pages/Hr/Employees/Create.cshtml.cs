using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.Employees;

[Authorize(Policy = ErpPermissions.Employees.Create)]
public class CreateModel : AbpPageModel
{
    private readonly EmployeeAppService _employeeAppService;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(EmployeeAppService employeeAppService, IFeatureChecker featureChecker)
    {
        _employeeAppService = employeeAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateEmployeeDto Employee { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var employee = await _employeeAppService.CreateAsync(Employee);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = employee.Id }) });
        }

        return RedirectToPage("./Detail", new { id = employee.Id });
    }
}
