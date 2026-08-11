using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Customers;

public class LeadImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedDuplicateTicket { get; set; }
    public int SkippedDuplicatePhone { get; set; }
    public int SkippedInvalidRow { get; set; }
    public int NewAgentsCreated { get; set; }
    public List<string> RowErrors { get; set; } = new();
}
