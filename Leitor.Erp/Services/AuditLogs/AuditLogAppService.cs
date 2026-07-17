using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.AuditLogging;

namespace Leitor.Erp.Services.AuditLogs;

// Thin read-only wrapper around ABP's own audit logging module (Volo.Abp.AuditLogging.EntityFrameworkCore
// is already recording every request/entity change into the same database - see ErpDbContext -
// there was just no UI to browse it). Nothing here writes audit log data; it only queries it.
[Authorize(ErpPermissions.AuditLogs.Default)]
public class AuditLogAppService : ApplicationService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogAppService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public virtual async Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogListInput input)
    {
        var totalCount = await _auditLogRepository.GetCountAsync(
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            userName: input.UserName,
            hasException: input.HasException);

        var logs = await _auditLogRepository.GetListAsync(
            sorting: string.IsNullOrWhiteSpace(input.Sorting) ? $"{nameof(AuditLog.ExecutionTime)} DESC" : input.Sorting,
            maxResultCount: input.MaxResultCount,
            skipCount: input.SkipCount,
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            userName: input.UserName,
            hasException: input.HasException);

        var items = logs.Select(MapToDto).ToList();
        return new PagedResultDto<AuditLogDto>(totalCount, items);
    }

    public virtual async Task<AuditLogDetailDto> GetAsync(Guid id)
    {
        var log = await _auditLogRepository.GetAsync(id);

        return new AuditLogDetailDto
        {
            Id = log.Id,
            UserName = log.UserName,
            ExecutionTime = log.ExecutionTime,
            ExecutionDuration = log.ExecutionDuration,
            ClientIpAddress = log.ClientIpAddress,
            HttpMethod = log.HttpMethod,
            Url = log.Url,
            HttpStatusCode = log.HttpStatusCode,
            HasException = !string.IsNullOrEmpty(log.Exceptions),
            BrowserInfo = log.BrowserInfo,
            Exceptions = log.Exceptions,
            Comments = log.Comments,
            Actions = log.Actions.Select(a => new AuditLogActionDto
            {
                ServiceName = a.ServiceName,
                MethodName = a.MethodName,
                ExecutionTime = a.ExecutionTime,
                ExecutionDuration = a.ExecutionDuration
            }).OrderBy(a => a.ExecutionTime).ToList(),
            EntityChanges = log.EntityChanges.Select(c => new EntityChangeDto
            {
                Id = c.Id,
                ChangeTime = c.ChangeTime,
                ChangeType = c.ChangeType,
                EntityId = c.EntityId,
                EntityTypeFullName = c.EntityTypeFullName,
                PropertyChanges = c.PropertyChanges.Select(p => new EntityPropertyChangeDto
                {
                    PropertyName = p.PropertyName,
                    OriginalValue = p.OriginalValue,
                    NewValue = p.NewValue
                }).ToList()
            }).OrderBy(c => c.ChangeTime).ToList()
        };
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserName = log.UserName,
            ExecutionTime = log.ExecutionTime,
            ExecutionDuration = log.ExecutionDuration,
            ClientIpAddress = log.ClientIpAddress,
            HttpMethod = log.HttpMethod,
            Url = log.Url,
            HttpStatusCode = log.HttpStatusCode,
            HasException = !string.IsNullOrEmpty(log.Exceptions)
        };
    }
}
