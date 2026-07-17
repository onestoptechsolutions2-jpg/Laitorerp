using System;

namespace Leitor.Erp.Services.Dtos.AuditLogs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? HttpStatusCode { get; set; }
    public bool HasException { get; set; }
}
