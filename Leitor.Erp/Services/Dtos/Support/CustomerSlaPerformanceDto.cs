namespace Leitor.Erp.Services.Dtos.Support;

// The per-customer counterpart to SlaBreachMonthDto (org-wide, monthly) - an all-time summary for
// one customer, the headline number an account manager hands a retainer client as their own
// Experience Level Agreement performance (ITIL v5 formalizes this as its own concept). All-time
// rather than a trailing window since a smaller-volume client might only have a handful of tickets
// in any given month - windowing would make the number noisy rather than useful.
public class CustomerSlaPerformanceDto
{
    public int TotalCount { get; set; }
    public int MetCount { get; set; }
    public int BreachedCount { get; set; }
    public decimal? CompliancePercent { get; set; }
}
