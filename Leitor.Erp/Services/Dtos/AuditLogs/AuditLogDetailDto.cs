using System;
using System.Collections.Generic;
using Volo.Abp.Auditing;

namespace Leitor.Erp.Services.Dtos.AuditLogs;

public class AuditLogDetailDto : AuditLogDto
{
    public string? BrowserInfo { get; set; }
    public string? Exceptions { get; set; }
    public string? Comments { get; set; }
    public List<AuditLogActionDto> Actions { get; set; } = new();
    public List<EntityChangeDto> EntityChanges { get; set; } = new();
}

public class AuditLogActionDto
{
    public string ServiceName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
}

public class EntityChangeDto
{
    public Guid Id { get; set; }
    public DateTime ChangeTime { get; set; }
    public EntityChangeType ChangeType { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string EntityTypeFullName { get; set; } = string.Empty;
    public List<EntityPropertyChangeDto> PropertyChanges { get; set; } = new();
}

public class EntityPropertyChangeDto
{
    public string? PropertyName { get; set; }
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
}
