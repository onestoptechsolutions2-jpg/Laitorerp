using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Sales.Analytics;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class IndexModel : AbpPageModel
{
    private readonly SalesAnalyticsAppService _salesAnalyticsAppService;

    public IndexModel(SalesAnalyticsAppService salesAnalyticsAppService)
    {
        _salesAnalyticsAppService = salesAnalyticsAppService;
    }

    public bool CanViewLeadFunnel { get; set; }
    public bool CanViewWinRateTrend { get; set; }

    public List<LeadFunnelStageDto> LeadFunnel { get; set; } = new();
    public List<WinRateMonthDto> WinRateTrend { get; set; } = new();
    public List<SalesAgingBucketDto> Aging { get; set; } = new();

    public async Task OnGetAsync()
    {
        CanViewLeadFunnel = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Default);
        CanViewWinRateTrend = await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Default);

        if (CanViewLeadFunnel)
        {
            LeadFunnel = await _salesAnalyticsAppService.GetLeadFunnelAsync();
        }

        if (CanViewWinRateTrend)
        {
            WinRateTrend = await _salesAnalyticsAppService.GetWinRateTrendAsync();
        }

        Aging = await _salesAnalyticsAppService.GetAgingAsync();
    }
}
