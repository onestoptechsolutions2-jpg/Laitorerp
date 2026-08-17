using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.Hr;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class PayrollRunDto : FullAuditedEntityDto<Guid>
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PayrollRunStatus Status { get; set; }
    public DateTime? RunAt { get; set; }
    public Guid? RunByUserId { get; set; }
    public string? RunByUserName { get; set; }
    public decimal TotalNetPay { get; set; }
    public int EmployeeCount { get; set; }

    public List<PayrollRunLineDto> Lines { get; set; } = new();
}

public class PayrollRunLineDto : FullAuditedEntityDto<Guid>
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
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
}
