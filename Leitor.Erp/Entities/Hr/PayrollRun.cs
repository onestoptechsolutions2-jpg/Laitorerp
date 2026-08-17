using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Hr;

public class PayrollRun : FullAuditedAggregateRoot<Guid>
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public DateTime? RunAt { get; set; }
    public Guid? RunByUserId { get; set; }

    protected PayrollRun()
    {
    }

    public PayrollRun(Guid id, DateTime periodStart, DateTime periodEnd)
        : base(id)
    {
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }
}
