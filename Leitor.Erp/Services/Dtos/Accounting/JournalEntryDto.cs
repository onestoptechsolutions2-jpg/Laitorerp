using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class JournalEntryDto : FullAuditedEntityDto<Guid>
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public bool IsSystemGenerated { get; set; }

    // Resolved by JournalEntryAppService - not stored columns.
    public List<JournalEntryLineDto> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}
