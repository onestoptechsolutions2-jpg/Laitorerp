using System;
using Leitor.Erp.Entities.FieldService;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class FieldServiceJobNoteDto : FullAuditedEntityDto<Guid>
{
    public Guid JobId { get; set; }
    public FieldServiceJobNoteType Type { get; set; }
    public string Text { get; set; } = string.Empty;

    // Resolved by FieldServiceJobNoteAppService from IdentityUser repository - not a stored column.
    public string? CreatorUserName { get; set; }
}
