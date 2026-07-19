using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Sales;

public class LeadFunnelStageDto
{
    public LeadStatus Status { get; set; }
    public int Count { get; set; }
}

public class WinRateMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int WonCount { get; set; }
    public int LostCount { get; set; }
}

// Buckets: 0-7, 8-30, 31+ days since creation.
public class SalesAgingBucketDto
{
    public string Bucket { get; set; } = string.Empty;
    public int QuoteCount { get; set; }
    public int OrderCount { get; set; }
}
