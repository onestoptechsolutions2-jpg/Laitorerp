using System;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class GetJournalEntryListInput
{
    public Guid? AccountId { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 20;
}
