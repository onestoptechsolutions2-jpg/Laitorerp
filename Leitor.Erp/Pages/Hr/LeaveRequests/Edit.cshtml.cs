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

[Authorize(Policy = ErpPermissions.Leave.Edit)]
public class EditModel : AbpPageModel
{
    private readonly LeaveRequestAppService _leaveRequestAppService;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
        LeaveRequestAppService leaveRequestAppService,
        IRepository<Employee, Guid> employeeRepository,
        IFeatureChecker featureChecker)
    {
        _leaveRequestAppService = leaveRequestAppService;
        _employeeRepository = employeeRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateLeaveRequestDto LeaveRequest { get; set; } = new();

    public List<SelectListItem> EmployeeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        var leaveRequest = await _leaveRequestAppService.GetAsync(Id);
        LeaveRequest = new CreateUpdateLeaveRequestDto
        {
            EmployeeId = leaveRequest.EmployeeId,
            LeaveType = leaveRequest.LeaveType,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            DaysRequested = leaveRequest.DaysRequested,
            Reason = leaveRequest.Reason
        };

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

        await _leaveRequestAppService.UpdateAsync(Id, LeaveRequest);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = Id }) });
        }

        return RedirectToPage("./Detail", new { id = Id });
    }

    private async Task LoadOptionsAsync()
    {
        var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
        EmployeeOptions = employees.OrderBy(x => x.FullName).Select(x => new SelectListItem(x.FullName, x.Id.ToString())).ToList();
    }
}
