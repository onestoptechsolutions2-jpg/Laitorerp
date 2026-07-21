using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Support.Analytics;

[Authorize(Policy = ErpPermissions.Support.Default)]
public class IndexModel : AbpPageModel
{
    private readonly SupportAnalyticsAppService _supportAnalyticsAppService;

    public IndexModel(SupportAnalyticsAppService supportAnalyticsAppService)
    {
        _supportAnalyticsAppService = supportAnalyticsAppService;
    }

    public List<TicketVolumeMonthDto> VolumeTrend { get; set; } = new();
    public List<SlaBreachMonthDto> SlaBreachTrend { get; set; } = new();
    public List<CsatMonthDto> CsatTrend { get; set; } = new();

    public async Task OnGetAsync()
    {
        VolumeTrend = await _supportAnalyticsAppService.GetTicketVolumeTrendAsync();
        SlaBreachTrend = await _supportAnalyticsAppService.GetSlaBreachTrendAsync();
        CsatTrend = await _supportAnalyticsAppService.GetCsatTrendAsync();
    }
}
