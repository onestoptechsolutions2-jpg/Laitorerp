using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// WorkflowMonitorAppService/DashboardAppService. Each method checks its own module permission so
// the page can show whichever sections the current user actually has access to (a Sales-only role
// shouldn't be blocked from the whole page, but also shouldn't silently see Lead data without
// Leads.Default - the same fix applied to Customer Detail's 360 view earlier in this initiative).
public class SalesAnalyticsAppService : ApplicationService
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Order, Guid> _orderRepository;

    public SalesAnalyticsAppService(
        IRepository<Lead, Guid> leadRepository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Order, Guid> orderRepository)
    {
        _leadRepository = leadRepository;
        _opportunityRepository = opportunityRepository;
        _quoteRepository = quoteRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<LeadFunnelStageDto>> GetLeadFunnelAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Leads.Default);

        var leads = await _leadRepository.GetListAsync();
        return leads
            .GroupBy(x => x.Status)
            .Select(g => new LeadFunnelStageDto { Status = g.Key, Count = g.Count() })
            .OrderBy(x => x.Status)
            .ToList();
    }

    public async Task<List<WinRateMonthDto>> GetWinRateTrendAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Default);

        var since = Clock.Now.AddMonths(-11);
        var closedOpportunities = await _opportunityRepository.GetListAsync(
            x => x.ClosedDate.HasValue && x.ClosedDate.Value >= since &&
                 (x.Status == OpportunityStatus.Won || x.Status == OpportunityStatus.Lost));

        var byMonth = closedOpportunities
            .GroupBy(x => new { x.ClosedDate!.Value.Year, x.ClosedDate.Value.Month })
            .ToDictionary(g => g.Key, g => new WinRateMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                WonCount = g.Count(x => x.Status == OpportunityStatus.Won),
                LostCount = g.Count(x => x.Status == OpportunityStatus.Lost)
            });

        var result = new List<WinRateMonthDto>();
        var cursor = new DateTime(since.Year, since.Month, 1);
        var end = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        while (cursor <= end)
        {
            result.Add(byMonth.TryGetValue(new { cursor.Year, cursor.Month }, out var month)
                ? month
                : new WinRateMonthDto { Year = cursor.Year, Month = cursor.Month });
            cursor = cursor.AddMonths(1);
        }

        return result;
    }

    public async Task<List<SalesAgingBucketDto>> GetAgingAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Default);

        var openQuotes = await _quoteRepository.GetListAsync(x => x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent);
        var openOrders = await _orderRepository.GetListAsync(x => x.Status == OrderStatus.Submitted);

        var now = Clock.Now;
        var buckets = new[] { "0-7", "8-30", "31+" };

        string BucketFor(DateTime creationTime)
        {
            var days = (now - creationTime).TotalDays;
            return days <= 7 ? "0-7" : days <= 30 ? "8-30" : "31+";
        }

        var quotesByBucket = openQuotes.GroupBy(x => BucketFor(x.CreationTime)).ToDictionary(g => g.Key, g => g.Count());
        var ordersByBucket = openOrders.GroupBy(x => BucketFor(x.CreationTime)).ToDictionary(g => g.Key, g => g.Count());

        return buckets.Select(bucket => new SalesAgingBucketDto
        {
            Bucket = bucket,
            QuoteCount = quotesByBucket.GetValueOrDefault(bucket),
            OrderCount = ordersByBucket.GetValueOrDefault(bucket)
        }).ToList();
    }
}
