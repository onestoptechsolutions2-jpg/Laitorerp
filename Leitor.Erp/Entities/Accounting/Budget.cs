using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// One row per Account per FiscalYear/Month - the granularity BudgetVarianceReportAppService needs
// to compare against actuals for any single month or a whole year. Entered via a spreadsheet-style
// grid (Pages/Accounting/Budgets/Index) rather than one row at a time.
public class Budget : FullAuditedAggregateRoot<Guid>
{
    public Guid AccountId { get; set; }
    public int FiscalYear { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }

    protected Budget()
    {
    }

    public Budget(Guid id, Guid accountId, int fiscalYear, int month, decimal amount)
        : base(id)
    {
        AccountId = accountId;
        FiscalYear = fiscalYear;
        Month = month;
        Amount = amount;
    }
}
