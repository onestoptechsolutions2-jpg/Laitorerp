using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.BudgetVariance;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly BudgetVarianceReportAppService _budgetVarianceReportAppService;

    public IndexModel(BudgetVarianceReportAppService budgetVarianceReportAppService)
    {
        _budgetVarianceReportAppService = budgetVarianceReportAppService;
    }

    [BindProperty(SupportsGet = true)]
    public int FiscalYear { get; set; } = DateTime.Today.Year;

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    public BudgetVarianceReportDto Report { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Report = await _budgetVarianceReportAppService.GetVarianceAsync(FiscalYear, Month);
    }
}
