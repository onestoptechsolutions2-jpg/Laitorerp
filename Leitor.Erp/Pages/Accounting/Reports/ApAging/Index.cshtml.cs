using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.ApAging;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly AgingReportAppService _agingReportAppService;

    public IndexModel(AgingReportAppService agingReportAppService)
    {
        _agingReportAppService = agingReportAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime AsOfDate { get; set; } = DateTime.Today;

    public AgingReportDto Report { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Report = await _agingReportAppService.GetApAgingAsync(AsOfDate);
    }
}
