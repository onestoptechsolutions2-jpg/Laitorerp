using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Dashboard;

// Read-only aggregate for the home page overview. Each section is only populated if the current
// user has at least view access to that module, matching the same permission checks the module's
// own list pages use - so the dashboard never surfaces counts the user couldn't otherwise see.
public class DashboardAppService : ApplicationService
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<LeadTouch, Guid> _leadTouchRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;

    public DashboardAppService(
        IRepository<Lead, Guid> leadRepository,
        IRepository<LeadTouch, Guid> leadTouchRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository)
    {
        _leadRepository = leadRepository;
        _leadTouchRepository = leadTouchRepository;
        _customerRepository = customerRepository;
        _opportunityRepository = opportunityRepository;
        _jobRepository = jobRepository;
        _quoteRepository = quoteRepository;
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<DashboardDto> GetAsync()
    {
        var dto = new DashboardDto();

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Default))
        {
            dto.Leads = await GetLeadStatsAsync();
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Default))
        {
            dto.Customers = await GetCustomerStatsAsync();
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Default))
        {
            dto.Opportunities = await GetOpportunityStatsAsync();
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Default))
        {
            dto.FieldService = await GetFieldServiceStatsAsync();
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Default))
        {
            dto.Sales = await GetSalesStatsAsync();
        }

        return dto;
    }

    private async Task<LeadStatsDto> GetLeadStatsAsync()
    {
        var leadQuery = await _leadRepository.GetQueryableAsync();
        var weekAgo = Clock.Now.AddDays(-7);
        var monthAgo = Clock.Now.AddMonths(-1);

        var touchesThisWeek = await _leadTouchRepository.GetListAsync(x => x.TouchedAt >= weekAgo);

        return new LeadStatsDto
        {
            TotalCount = leadQuery.Count(),
            NewCount = leadQuery.Count(x => x.Status == LeadStatus.New),
            TouchesThisWeek = touchesThisWeek.Count,
            ReplyRate = touchesThisWeek.Count == 0
                ? 0
                : Math.Round(100m * touchesThisWeek.Count(x => x.Direction == LeadDirection.Inbound) / touchesThisWeek.Count, 1),
            ConvertedThisMonth = leadQuery.Count(x => x.Status == LeadStatus.Converted && x.LastModificationTime >= monthAgo)
        };
    }

    private async Task<CustomerStatsDto> GetCustomerStatsAsync()
    {
        var query = await _customerRepository.GetQueryableAsync();

        return new CustomerStatsDto
        {
            TotalCount = query.Count(),
            Recent = query
                .OrderByDescending(x => x.CreationTime)
                .Take(5)
                .Select(x => new RecentCustomerDto { Id = x.Id, Name = x.Name, CreationTime = x.CreationTime })
                .ToList()
        };
    }

    private async Task<OpportunityStatsDto> GetOpportunityStatsAsync()
    {
        var query = await _opportunityRepository.GetQueryableAsync();

        var wonCount = query.Count(x => x.Status == OpportunityStatus.Won);
        var lostCount = query.Count(x => x.Status == OpportunityStatus.Lost);
        var decidedCount = wonCount + lostCount;

        return new OpportunityStatsDto
        {
            OpenCount = query.Count(x => x.Status == OpportunityStatus.Open),
            OpenPipelineValue = query.Where(x => x.Status == OpportunityStatus.Open).Sum(x => x.EstimatedValue ?? 0),
            WonCount = wonCount,
            LostCount = lostCount,
            WinRate = decidedCount == 0 ? 0 : Math.Round(100m * wonCount / decidedCount, 1)
        };
    }

    private async Task<FieldServiceStatsDto> GetFieldServiceStatsAsync()
    {
        var query = await _jobRepository.GetQueryableAsync();
        var weekAhead = Clock.Now.AddDays(7);

        var upcomingJobs = query
            .Where(x => x.Status == FieldServiceJobStatus.Scheduled || x.Status == FieldServiceJobStatus.InProgress)
            .OrderBy(x => x.ScheduledDate)
            .Take(5)
            .ToList();

        var customerIds = upcomingJobs.Select(x => x.CustomerId).Distinct().ToList();
        var namesById = customerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);

        return new FieldServiceStatsDto
        {
            ScheduledCount = query.Count(x => x.Status == FieldServiceJobStatus.Scheduled),
            InProgressCount = query.Count(x => x.Status == FieldServiceJobStatus.InProgress),
            UpcomingCount = query.Count(x =>
                (x.Status == FieldServiceJobStatus.Scheduled || x.Status == FieldServiceJobStatus.InProgress) &&
                x.ScheduledDate <= weekAhead),
            Upcoming = upcomingJobs.Select(x => new UpcomingJobDto
            {
                Id = x.Id,
                CustomerName = namesById.GetValueOrDefault(x.CustomerId, string.Empty),
                Type = x.Type,
                Status = x.Status,
                ScheduledDate = x.ScheduledDate
            }).ToList()
        };
    }

    private async Task<SalesStatsDto> GetSalesStatsAsync()
    {
        var quoteQuery = await _quoteRepository.GetQueryableAsync();
        var orderQuery = await _orderRepository.GetQueryableAsync();

        var openQuoteCount = quoteQuery.Count(x => x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent);
        var pendingOrderCount = orderQuery.Count(x => x.Status == OrderStatus.Submitted || x.Status == OrderStatus.Confirmed);

        // Payment status/amounts are always computed from lines + payments, never stored - same
        // approach as InvoiceAppService.ComputePaymentStatus. Draft/Cancelled invoices aren't
        // billed yet so they're excluded from outstanding-balance figures.
        var invoices = await _invoiceRepository.GetListAsync(
            x => x.Status != InvoiceStatus.Cancelled && x.Status != InvoiceStatus.Draft);
        var invoiceIds = invoices.Select(x => x.Id).ToList();

        var allLines = await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));
        var linesByInvoiceId = allLines.ToLookup(x => x.InvoiceId);

        var allPayments = await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));
        var paymentsByInvoiceId = allPayments.ToLookup(x => x.InvoiceId);

        var customerIds = invoices.Select(x => x.CustomerId).Distinct().ToList();
        var namesById = customerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);

        var now = Clock.Now;
        var unpaidInvoiceCount = 0;
        var overdueInvoiceCount = 0;
        var outstandingAmount = 0m;
        var overdueInvoices = new List<OverdueInvoiceDto>();

        foreach (var invoice in invoices)
        {
            var total = linesByInvoiceId[invoice.Id]
                .Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m) * (1 + x.TaxRatePercent / 100m));
            var amountPaid = paymentsByInvoiceId[invoice.Id].Sum(x => x.Amount);
            var amountDue = total - amountPaid;

            if (amountDue <= 0)
            {
                continue;
            }

            unpaidInvoiceCount++;
            outstandingAmount += amountDue;

            if (invoice.DueDate < now)
            {
                overdueInvoiceCount++;
                overdueInvoices.Add(new OverdueInvoiceDto
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    CustomerName = namesById.GetValueOrDefault(invoice.CustomerId, string.Empty),
                    DueDate = invoice.DueDate,
                    AmountDue = amountDue
                });
            }
        }

        return new SalesStatsDto
        {
            OpenQuoteCount = openQuoteCount,
            PendingOrderCount = pendingOrderCount,
            UnpaidInvoiceCount = unpaidInvoiceCount,
            OverdueInvoiceCount = overdueInvoiceCount,
            OutstandingAmount = outstandingAmount,
            OverdueInvoices = overdueInvoices.OrderBy(x => x.DueDate).Take(5).ToList()
        };
    }
}
