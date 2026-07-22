using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class BudgetVarianceReportDto
{
    public int FiscalYear { get; set; }
    public int? Month { get; set; }
    public List<BudgetVarianceLineDto> RevenueLines { get; set; } = new();
    public List<BudgetVarianceLineDto> ExpenseLines { get; set; } = new();
}

public class BudgetVarianceLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Actual { get; set; }
    public decimal Budget { get; set; }
    public decimal Variance { get; set; }

    // Null when Budget is 0 - a percentage against a zero budget is meaningless, not "infinite%".
    public decimal? VariancePercent { get; set; }
}
