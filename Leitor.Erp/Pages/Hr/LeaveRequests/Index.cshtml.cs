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

[Authorize(Policy = ErpPermissions.Leave.Default)]
public class IndexModel : AbpPageModel
{
    private readonly LeaveRequestAppService _leaveRequestAppService;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(
        LeaveRequestAppService leaveRequestAppService,
        IRepository<Employee, Guid> employeeRepository,
        IFeatureChecker featureChecker)
    {
        _leaveRequestAppService = leaveRequestAppService;
        _employeeRepository = employeeRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? EmployeeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public LeaveRequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<LeaveRequestDto> LeaveRequests { get; set; } = Array.Empty<LeaveRequestDto>();
    public PaginationModel Pagination { get; set; } = new();
    public List<SelectListItem> EmployeeOptions { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leave.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leave.Edit);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _leaveRequestAppService.GetListAsync(new GetLeaveRequestListInput
        {
            EmployeeId = EmployeeId,
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        LeaveRequests = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };

        var employees = await _employeeRepository.GetListAsync();
        EmployeeOptions = new List<SelectListItem> { new(L["All"], "") };
        EmployeeOptions.AddRange(employees.OrderBy(x => x.FullName).Select(x => new SelectListItem(x.FullName, x.Id.ToString())));

        return Page();
    }
}
