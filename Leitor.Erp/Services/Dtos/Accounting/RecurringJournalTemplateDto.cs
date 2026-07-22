using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class RecurringJournalTemplateDto : FullAuditedEntityDto<Guid>
{
    public string Description { get; set; } = string.Empty;
    public RecurringJournalFrequency Frequency { get; set; }
    public DateTime NextRunDate { get; set; }
    public bool IsActive { get; set; }
    public List<RecurringJournalTemplateLineDto> Lines { get; set; } = new();
}

public class RecurringJournalTemplateLineDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    // Resolved by RecurringJournalTemplateAppService - not a stored column.
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
}
