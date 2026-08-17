using System;
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

[Authorize(Policy = ErpPermissions.Employees.Edit)]
public class EditModel : AbpPageModel
{
    private readonly EmployeeAppService _employeeAppService;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(EmployeeAppService employeeAppService, IFeatureChecker featureChecker)
    {
        _employeeAppService = employeeAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateEmployeeDto Employee { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        var employee = await _employeeAppService.GetAsync(Id);
        Employee = new CreateUpdateEmployeeDto
        {
            FullName = employee.FullName,
            Email = employee.Email,
            Phone = employee.Phone,
            UserId = employee.UserId,
            JobTitle = employee.JobTitle,
            Department = employee.Department,
            HireDate = employee.HireDate,
            TerminationDate = employee.TerminationDate,
            IsActive = employee.IsActive,
            KraPin = employee.KraPin,
            NssfNumber = employee.NssfNumber,
            ShaNumber = employee.ShaNumber,
            BasicSalary = employee.BasicSalary,
            BankName = employee.BankName,
            BankAccountNumber = employee.BankAccountNumber
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _employeeAppService.UpdateAsync(Id, Employee);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = Id }) });
        }

        return RedirectToPage("./Detail", new { id = Id });
    }
}
