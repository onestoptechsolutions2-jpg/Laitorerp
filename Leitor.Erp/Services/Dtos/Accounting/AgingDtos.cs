using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class AgingReportDto
{
    public DateTime AsOfDate { get; set; }
    public List<AgingRowDto> Rows { get; set; } = new();
    public decimal TotalCurrent { get; set; }
    public decimal TotalDays1To30 { get; set; }
    public decimal TotalDays31To60 { get; set; }
    public decimal TotalDays61To90 { get; set; }
    public decimal TotalOver90 { get; set; }
    public decimal GrandTotal { get; set; }
}

// One row per Customer (AR) or Vendor (AP) with an open balance, bucketed by days overdue from
// each document's own DueDate.
public class AgingRowDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal Total { get; set; }
}
