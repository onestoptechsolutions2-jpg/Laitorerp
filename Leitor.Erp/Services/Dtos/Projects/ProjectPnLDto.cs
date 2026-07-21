using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Projects;

public class ProjectPnLDto
{
    public Guid ProjectId { get; set; }
    public List<ProjectPnLLineDto> RevenueLines { get; set; } = new();
    public List<ProjectPnLLineDto> ExpenseLines { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
}

public class ProjectPnLLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
