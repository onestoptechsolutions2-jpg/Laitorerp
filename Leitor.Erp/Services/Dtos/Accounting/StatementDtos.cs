using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class StatementDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<StatementLineDto> Lines { get; set; } = new();
}

// Charge (invoice) or Credit (payment) - exactly one of the two is non-zero per line, mirroring
// the Debit/Credit shape of a JournalEntryLine.
public class StatementLineDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Charge { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
