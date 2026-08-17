using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Hr;

// One row per employee in a PayrollRun - every component is stored explicitly (not just NetPay)
// so a payslip or audit later doesn't need to re-run PayeCalculator against whatever the tax
// bands happen to be at that later point in time.
public class PayrollRunLine : FullAuditedAggregateRoot<Guid>
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }

    public decimal GrossPay { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal Paye { get; set; }
    public decimal PersonalRelief { get; set; }
    public decimal NssfEmployee { get; set; }
    public decimal NssfEmployer { get; set; }
    public decimal ShaContribution { get; set; }
    public decimal HousingLevyEmployee { get; set; }
    public decimal HousingLevyEmployer { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal NetPay { get; set; }

    protected PayrollRunLine()
    {
    }

    public PayrollRunLine(Guid id, Guid payrollRunId, Guid employeeId)
        : base(id)
    {
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
    }
}
