using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.LeaveRequests;

[Authorize(Policy = ErpPermissions.Leave.Create)]
public class CreateModel : AbpPageModel
{
    private readonly LeaveRequestAppService _leaveRequestAppService;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        LeaveRequestAppService leaveRequestAppService,
        IRepository<Employee, Guid> employeeRepository,
        IFeatureChecker featureChecker)
    {
        _leaveRequestAppService = leaveRequestAppService;
        _employeeRepository = employeeRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateLeaveRequestDto LeaveRequest { get; set; } = new();

    public List<SelectListItem> EmployeeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var leaveRequest = await _leaveRequestAppService.CreateAsync(LeaveRequest);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = leaveRequest.Id }) });
        }

        return RedirectToPage("./Detail", new { id = leaveRequest.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
        EmployeeOptions = employees.OrderBy(x => x.FullName).Select(x => new SelectListItem(x.FullName, x.Id.ToString())).ToList();
    }
}
