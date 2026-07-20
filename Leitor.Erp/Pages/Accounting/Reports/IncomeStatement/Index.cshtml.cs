using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.IncomeStatement;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly GeneralLedgerReportAppService _reportAppService;

    public IndexModel(GeneralLedgerReportAppService reportAppService)
    {
        _reportAppService = reportAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; } = new(DateTime.Today.Year, 1, 1);

    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; } = DateTime.Today;

    public IncomeStatementDto Report { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Report = await _reportAppService.GetIncomeStatementAsync(FromDate, ToDate);
    }
}
