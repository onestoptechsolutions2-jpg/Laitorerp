using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

[Authorize(Policy = ErpPermissions.Employees.Default)]
public class IndexModel : AbpPageModel
{
    private readonly EmployeeAppService _employeeAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(EmployeeAppService employeeAppService, IFeatureChecker featureChecker)
    {
        _employeeAppService = employeeAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<EmployeeDto> Employees { get; set; } = Array.Empty<EmployeeDto>();
    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Employees.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Employees.Edit);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Employees.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _employeeAppService.GetListAsync(new GetEmployeeListInput
        {
            Filter = Filter,
            IsActive = IsActive,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Employees = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _employeeAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, IsActive, PageIndex });
    }
}
