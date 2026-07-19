using System;
using System.Collections.Generic;
using Leitor.Erp.Entities.FieldService;

namespace Leitor.Erp.Services.Dtos.Dashboard;

public class DashboardDto
{
    public LeadStatsDto? Leads { get; set; }
    public CustomerStatsDto? Customers { get; set; }
    public OpportunityStatsDto? Opportunities { get; set; }
    public FieldServiceStatsDto? FieldService { get; set; }
    public SalesStatsDto? Sales { get; set; }
}

public class OpportunityStatsDto
{
    public int OpenCount { get; set; }
    public decimal OpenPipelineValue { get; set; }
    public int WonCount { get; set; }
    public int LostCount { get; set; }

    // Won / (Won + Lost), trailing period - 0 when there's no decided history yet.
    public decimal WinRate { get; set; }
}

public class LeadStatsDto
{
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int TouchesThisWeek { get; set; }
    public decimal ReplyRate { get; set; }
    public int ConvertedThisMonth { get; set; }
}

public class CustomerStatsDto
{
    public int TotalCount { get; set; }
    public List<RecentCustomerDto> Recent { get; set; } = new();
}

public class RecentCustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
}

public class FieldServiceStatsDto
{
    public int ScheduledCount { get; set; }
    public int InProgressCount { get; set; }
    public int UpcomingCount { get; set; }
    public List<UpcomingJobDto> Upcoming { get; set; } = new();
}

public class UpcomingJobDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public FieldServiceJobType Type { get; set; }
    public FieldServiceJobStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
}

public class SalesStatsDto
{
    public int OpenQuoteCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int UnpaidInvoiceCount { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public List<OverdueInvoiceDto> OverdueInvoices { get; set; } = new();
}

public class OverdueInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
}
