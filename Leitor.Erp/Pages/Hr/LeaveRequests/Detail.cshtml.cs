using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.LeaveRequests;

[Authorize(Policy = ErpPermissions.Leave.Default)]
public class DetailModel : AbpPageModel
{
    private readonly LeaveRequestAppService _leaveRequestAppService;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<EscalationItem, Guid> _escalationItemRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        LeaveRequestAppService leaveRequestAppService,
        IRepository<Employee, Guid> employeeRepository,
        IRepository<EscalationItem, Guid> escalationItemRepository,
        IFeatureChecker featureChecker)
    {
        _leaveRequestAppService = leaveRequestAppService;
        _employeeRepository = employeeRepository;
        _escalationItemRepository = escalationItemRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public LeaveRequestDto LeaveRequest { get; set; } = null!;
    public string? EmployeeName { get; set; }
    public bool CanEdit { get; set; }
    public bool HasPendingApprovalEscalation { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leave.Edit);
        LeaveRequest = await _leaveRequestAppService.GetAsync(Id);

        var employee = await _employeeRepository.GetAsync(LeaveRequest.EmployeeId);
        EmployeeName = employee.FullName;

        HasPendingApprovalEscalation = await EscalationGate.IsPendingAsync(
            _escalationItemRepository, LeaveRequestAppService.ApproveActionType, Id);

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync()
    {
        try
        {
            await _leaveRequestAppService.SubmitAsync(Id);
        }
        catch (UserFriendlyException ex)
        {
            // EscalationGate.FileAsync always throws once it successfully files - this is the
            // expected "submitted" signal, not a real error, same pattern
            // OrderAppService.ConfirmAsync's own margin-gate escalation catch uses.
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }
}
