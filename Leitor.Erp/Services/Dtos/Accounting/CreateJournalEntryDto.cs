using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateJournalEntryDto
{
    [Required]
    public DateTime EntryDate { get; set; }

    [Required]
    [StringLength(256)]
    public string Description { get; set; } = string.Empty;

    public List<CreateJournalEntryLineDto> Lines { get; set; } = new();
}

public class CreateJournalEntryLineDto
{
    [Required]
    public Guid AccountId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Debit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Credit { get; set; }

    [Required]
    [StringLength(8)]
    public string CurrencyCode { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
}
