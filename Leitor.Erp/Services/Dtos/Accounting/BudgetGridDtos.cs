using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class BudgetGridDto
{
    public int FiscalYear { get; set; }
    public List<BudgetGridRowDto> Rows { get; set; } = new();
}

public class BudgetGridRowDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    // Index 0 = January ... index 11 = December, matching Budget.Month (1-12) minus one.
    public decimal[] MonthAmounts { get; set; } = new decimal[12];
}

public class SaveBudgetGridDto
{
    public int FiscalYear { get; set; }
    public List<BudgetCellDto> Cells { get; set; } = new();
}

public class BudgetCellDto
{
    public Guid AccountId { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
}
