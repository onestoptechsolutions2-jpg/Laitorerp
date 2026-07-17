using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.AuditLogs;
using Leitor.Erp.Services.Dtos.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.AuditLogs;

[Authorize(Policy = ErpPermissions.AuditLogs.Default)]
public class IndexModel : AbpPageModel
{
    private readonly AuditLogAppService _auditLogAppService;

    public IndexModel(AuditLogAppService auditLogAppService)
    {
        _auditLogAppService = auditLogAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartTime { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndTime { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UserName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? HttpMethod { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? HasException { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<AuditLogDto> AuditLogs { get; set; } = Array.Empty<AuditLogDto>();

    public PaginationModel Pagination { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _auditLogAppService.GetListAsync(new GetAuditLogListInput
        {
            StartTime = StartTime,
            EndTime = EndTime,
            UserName = UserName,
            HttpMethod = HttpMethod,
            HasException = HasException,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        AuditLogs = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }
}
