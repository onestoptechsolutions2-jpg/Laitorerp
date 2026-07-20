using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.BalanceSheet;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly GeneralLedgerReportAppService _reportAppService;

    public IndexModel(GeneralLedgerReportAppService reportAppService)
    {
        _reportAppService = reportAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime AsOfDate { get; set; } = DateTime.Today;

    public BalanceSheetDto Report { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Report = await _reportAppService.GetBalanceSheetAsync(AsOfDate);
    }
}
