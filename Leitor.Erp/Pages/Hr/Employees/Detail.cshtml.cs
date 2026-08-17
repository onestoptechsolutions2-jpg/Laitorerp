using System;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.Employees;

[Authorize(Policy = ErpPermissions.Employees.Default)]
public class DetailModel : AbpPageModel
{
    private readonly EmployeeAppService _employeeAppService;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(EmployeeAppService employeeAppService, IFeatureChecker featureChecker)
    {
        _employeeAppService = employeeAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public EmployeeDto Employee { get; set; } = null!;
    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Employees.Edit);
        Employee = await _employeeAppService.GetAsync(Id);
        return Page();
    }
}
