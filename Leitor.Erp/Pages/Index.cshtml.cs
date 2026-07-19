using System.Threading.Tasks;
using Leitor.Erp.Services.Dashboard;
using Leitor.Erp.Services.Dtos.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages;

[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly DashboardAppService _dashboardAppService;

    public IndexModel(DashboardAppService dashboardAppService)
    {
        _dashboardAppService = dashboardAppService;
    }

    public DashboardDto Dashboard { get; set; } = new();

    public async Task OnGetAsync()
    {
        Dashboard = await _dashboardAppService.GetAsync();
    }
}
