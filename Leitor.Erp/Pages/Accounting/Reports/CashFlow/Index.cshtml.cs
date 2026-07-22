using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.CashFlow;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly CashFlowReportAppService _cashFlowReportAppService;

    public IndexModel(CashFlowReportAppService cashFlowReportAppService)
    {
        _cashFlowReportAppService = cashFlowReportAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, 1, 1);

    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; } = DateTime.Today;

    public CashFlowDto Report { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Report = await _cashFlowReportAppService.GetCashFlowAsync(FromDate, ToDate);
    }
}
