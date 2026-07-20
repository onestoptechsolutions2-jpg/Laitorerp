using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class TrialBalanceLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
}

public class TrialBalanceDto
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
}

public class IncomeStatementLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class IncomeStatementDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<IncomeStatementLineDto> RevenueLines { get; set; } = new();
    public List<IncomeStatementLineDto> ExpenseLines { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetIncome { get; set; }
}

public class BalanceSheetLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BalanceSheetDto
{
    public DateTime AsOfDate { get; set; }
    public List<BalanceSheetLineDto> AssetLines { get; set; } = new();
    public List<BalanceSheetLineDto> LiabilityLines { get; set; } = new();
    public List<BalanceSheetLineDto> EquityLines { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }

    // No period-close/retained-earnings-transfer process exists in this app yet (v1
    // simplification, documented in the Phase 3 plan) - this is net income since inception,
    // computed the same way GetIncomeStatementAsync does, folded in here so Assets actually
    // equals Liabilities + Equity + this line.
    public decimal RetainedEarnings { get; set; }
}
