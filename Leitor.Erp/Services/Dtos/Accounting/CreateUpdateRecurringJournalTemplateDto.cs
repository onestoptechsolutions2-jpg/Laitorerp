using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Accounting;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateUpdateRecurringJournalTemplateDto
{
    [Required]
    [StringLength(256)]
    public string Description { get; set; } = string.Empty;

    public RecurringJournalFrequency Frequency { get; set; } = RecurringJournalFrequency.Monthly;

    [Required]
    public DateTime NextRunDate { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    public List<CreateUpdateRecurringJournalTemplateLineDto> Lines { get; set; } = new();
}

public class CreateUpdateRecurringJournalTemplateLineDto
{
    public Guid AccountId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Debit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Credit { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;
}
