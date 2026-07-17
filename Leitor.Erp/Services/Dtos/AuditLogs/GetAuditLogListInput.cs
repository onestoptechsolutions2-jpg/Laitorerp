using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.AuditLogs;

public class GetAuditLogListInput : PagedAndSortedResultRequestDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? UserName { get; set; }
    public string? HttpMethod { get; set; }
    public bool? HasException { get; set; }
}
