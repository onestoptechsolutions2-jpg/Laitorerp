using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Support;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// SalesAnalyticsAppService/GeneralLedgerReportAppService. Closes the ITIL4/ISO 20000 "Continual
// Improvement" gap flagged in the 2026-07-21 ITSM audit: Ticket.CustomerSatisfactionRating and
// SlaDueDate already existed but nothing aggregated them into a trend anyone could act on.
public class SupportAnalyticsAppService : ApplicationService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;

    public SupportAnalyticsAppService(IRepository<Ticket, Guid> ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<List<TicketVolumeMonthDto>> GetTicketVolumeTrendAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Support.Default);

        var since = Clock.Now.AddMonths(-11);
        var tickets = await _ticketRepository.GetListAsync(x => x.CreationTime >= since);

        var byMonth = tickets
            .GroupBy(x => (x.CreationTime.Year, x.CreationTime.Month))
            .ToDictionary(g => g.Key, g => g.Count());

        return BuildMonthlySeries(since, key => new TicketVolumeMonthDto
        {
            Year = key.Year,
            Month = key.Month,
            Count = byMonth.GetValueOrDefault(key)
        });
    }

    // "Breached" here means the ticket's own SLA target was missed by the time it was resolved (or,
    // for a still-open ticket, has already been missed as of now) - a stricter, historically
    // accurate measure than TicketDto.IsSlaBreached, which only flags tickets that are *currently*
    // open and overdue and silently reads as "not breached" the moment a late ticket gets resolved.
    public async Task<List<SlaBreachMonthDto>> GetSlaBreachTrendAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Support.Default);

        var since = Clock.Now.AddMonths(-11);
        var tickets = await _ticketRepository.GetListAsync(x => x.CreationTime >= since && x.SlaDueDate != null);
        var now = Clock.Now;

        bool WasBreached(Ticket ticket) => ticket.ResolvedDate.HasValue
            ? ticket.ResolvedDate.Value > ticket.SlaDueDate!.Value
            : now > ticket.SlaDueDate!.Value;

        var byMonth = tickets
            .GroupBy(x => (x.CreationTime.Year, x.CreationTime.Month))
            .ToDictionary(g => g.Key, g => new { Total = g.Count(), Breached = g.Count(WasBreached) });

        return BuildMonthlySeries(since, key =>
        {
            var month = byMonth.GetValueOrDefault(key);
            return new SlaBreachMonthDto
            {
                Year = key.Year,
                Month = key.Month,
                TotalCount = month?.Total ?? 0,
                BreachedCount = month?.Breached ?? 0
            };
        });
    }

    public async Task<List<CsatMonthDto>> GetCsatTrendAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Support.Default);

        var since = Clock.Now.AddMonths(-11);
        var tickets = await _ticketRepository.GetListAsync(
            x => x.ResolvedDate != null && x.ResolvedDate >= since && x.CustomerSatisfactionRating != null);

        var byMonth = tickets
            .GroupBy(x => (x.ResolvedDate!.Value.Year, x.ResolvedDate.Value.Month))
            .ToDictionary(g => g.Key, g => new { Average = g.Average(x => x.CustomerSatisfactionRating!.Value), Count = g.Count() });

        return BuildMonthlySeries(since, key =>
        {
            var month = byMonth.GetValueOrDefault(key);
            return new CsatMonthDto
            {
                Year = key.Year,
                Month = key.Month,
                AverageRating = month != null ? (decimal)month.Average : null,
                RatedCount = month?.Count ?? 0
            };
        });
    }

    // Shared "fill every month of the last 12, even the empty ones" cursor - same rolling-window
    // shape SalesAnalyticsAppService.GetWinRateTrendAsync already uses.
    private List<TResult> BuildMonthlySeries<TResult>(DateTime since, Func<(int Year, int Month), TResult> selector)
    {
        var result = new List<TResult>();
        var cursor = new DateTime(since.Year, since.Month, 1);
        var end = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        while (cursor <= end)
        {
            result.Add(selector((cursor.Year, cursor.Month)));
            cursor = cursor.AddMonths(1);
        }

        return result;
    }
}
