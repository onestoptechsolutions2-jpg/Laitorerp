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
using Volo.Abp.Application.Dtos;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.Payroll;

[Authorize(Policy = ErpPermissions.Payroll.Default)]
public class IndexModel : AbpPageModel
{
    private readonly PayrollRunAppService _payrollRunAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(PayrollRunAppService payrollRunAppService, IFeatureChecker featureChecker)
    {
        _payrollRunAppService = payrollRunAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    [BindProperty]
    public DateTime PeriodStart { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

    [BindProperty]
    public DateTime PeriodEnd { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1).AddDays(-1);

    public IReadOnlyList<PayrollRunDto> Runs { get; set; } = Array.Empty<PayrollRunDto>();
    public PaginationModel Pagination { get; set; } = new();
    public bool CanRun { get; set; }
    public bool CanManageRates { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        CanRun = await AuthorizationService.IsGrantedAsync(ErpPermissions.Payroll.Run);
        CanManageRates = await AuthorizationService.IsGrantedAsync(ErpPermissions.Payroll.ManageRates);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _payrollRunAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Runs = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostRunAsync()
    {
        try
        {
            var run = await _payrollRunAppService.RunAsync(PeriodStart, PeriodEnd);
            return RedirectToPage("./Detail", new { id = run.Id });
        }
        catch (Volo.Abp.UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(new { PageIndex });
        }
    }
}
