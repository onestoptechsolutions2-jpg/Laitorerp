using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class FiscalPeriodGridDto
{
    public int Year { get; set; }
    public List<FiscalPeriodRowDto> Months { get; set; } = new();
}

public class FiscalPeriodRowDto
{
    public int Month { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedDate { get; set; }
}
