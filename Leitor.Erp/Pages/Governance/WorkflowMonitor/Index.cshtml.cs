using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Governance.WorkflowMonitor;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class IndexModel : AbpPageModel
{
    private readonly WorkflowMonitorAppService _workflowMonitorAppService;

    public IndexModel(WorkflowMonitorAppService workflowMonitorAppService)
    {
        _workflowMonitorAppService = workflowMonitorAppService;
    }

    public IReadOnlyList<WorkflowMonitorRowDto> Rows { get; set; } = Array.Empty<WorkflowMonitorRowDto>();

    public async Task OnGetAsync()
    {
        Rows = await _workflowMonitorAppService.GetOverviewAsync();
    }
}
