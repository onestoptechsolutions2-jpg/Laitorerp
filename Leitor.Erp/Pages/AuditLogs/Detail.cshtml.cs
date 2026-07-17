using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.AuditLogs;
using Leitor.Erp.Services.Dtos.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.AuditLogs;

[Authorize(Policy = ErpPermissions.AuditLogs.Default)]
public class DetailModel : AbpPageModel
{
    private readonly AuditLogAppService _auditLogAppService;

    public DetailModel(AuditLogAppService auditLogAppService)
    {
        _auditLogAppService = auditLogAppService;
    }

    public AuditLogDetailDto AuditLog { get; set; } = null!;

    public async Task OnGetAsync([FromRoute] Guid id)
    {
        AuditLog = await _auditLogAppService.GetAsync(id);
    }
}
