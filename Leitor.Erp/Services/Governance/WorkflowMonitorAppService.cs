using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Governance;

// Aggregates the traceable chain each Opportunity has built up - Proposal -> Quote -> Order ->
// deposit/final invoicing -> installation - into one row per Opportunity for the Workflow Monitor
// page (Pages/Governance/WorkflowMonitor/Index.cshtml). Read-only: this never mutates anything,
// it just derives a current-stage/pending-action summary from data the other AppServices already
// maintain (entity Status fields + WorkflowStageEvent). Capped to the 100 most recently active
// Opportunities rather than fully paginated - a monitor is meant to show what's live right now,
// not every Opportunity ever created.
public class WorkflowMonitorAppService : ErpAppService
{
    private const int MaxRows = 100;

    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderPaymentMilestone, Guid> _milestoneRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<FieldServiceJob, Guid> _fieldServiceJobRepository;

    public WorkflowMonitorAppService(
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderPaymentMilestone, Guid> milestoneRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<FieldServiceJob, Guid> fieldServiceJobRepository)
    {
        _opportunityRepository = opportunityRepository;
        _customerRepository = customerRepository;
        _proposalRepository = proposalRepository;
        _quoteRepository = quoteRepository;
        _orderRepository = orderRepository;
        _milestoneRepository = milestoneRepository;
        _invoiceRepository = invoiceRepository;
        _fieldServiceJobRepository = fieldServiceJobRepository;
    }

    public async Task<List<WorkflowMonitorRowDto>> GetOverviewAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Default);

        var opportunities = (await _opportunityRepository.GetListAsync())
            .OrderByDescending(x => x.CreationTime)
            .Take(MaxRows)
            .ToList();
        var opportunityIds = opportunities.Select(x => x.Id).ToList();

        var customerIds = opportunities.Select(x => x.CustomerId).Distinct().ToList();
        var customerNamesById = (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.Name);

        var proposals = await _proposalRepository.GetListAsync(x => opportunityIds.Contains(x.OpportunityId));
        var latestProposalByOpportunityId = proposals
            .GroupBy(x => x.OpportunityId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreationTime).First());

        var proposalIds = proposals.Select(x => x.Id).ToList();
        var quotes = await _quoteRepository.GetListAsync(x => x.ProposalId.HasValue && proposalIds.Contains(x.ProposalId.Value));
        var quoteByProposalId = quotes.ToDictionary(x => x.ProposalId!.Value, x => x);

        var quoteIds = quotes.Select(x => x.Id).ToList();
        var orders = await _orderRepository.GetListAsync(x => x.QuoteId.HasValue && quoteIds.Contains(x.QuoteId.Value));
        var orderByQuoteId = orders.ToDictionary(x => x.QuoteId!.Value, x => x);

        var orderIds = orders.Select(x => x.Id).ToList();
        var milestones = await _milestoneRepository.GetListAsync(x => orderIds.Contains(x.OrderId));
        var milestonesByOrderId = milestones.ToLookup(x => x.OrderId);

        var invoices = await _invoiceRepository.GetListAsync(x => x.OrderId.HasValue && orderIds.Contains(x.OrderId.Value));
        var invoicesByOrderId = invoices.Where(x => x.OrderId.HasValue).ToLookup(x => x.OrderId!.Value);

        var jobs = await _fieldServiceJobRepository.GetListAsync(x => x.OrderId.HasValue && orderIds.Contains(x.OrderId.Value));
        var jobsByOrderId = jobs.Where(x => x.OrderId.HasValue).ToLookup(x => x.OrderId!.Value);

        var rows = new List<WorkflowMonitorRowDto>();

        foreach (var opportunity in opportunities)
        {
            var row = new WorkflowMonitorRowDto
            {
                OpportunityId = opportunity.Id,
                OpportunityName = opportunity.Name,
                CustomerName = customerNamesById.GetValueOrDefault(opportunity.CustomerId, string.Empty),
                OpportunityStatus = opportunity.Status
            };

            if (latestProposalByOpportunityId.TryGetValue(opportunity.Id, out var proposal))
            {
                row.ProposalId = proposal.Id;
                row.ProposalNumber = proposal.ProposalNumber;
                row.ProposalStatus = proposal.Status;

                if (quoteByProposalId.TryGetValue(proposal.Id, out var quote))
                {
                    row.QuoteId = quote.Id;
                    row.QuoteNumber = quote.QuoteNumber;
                    row.QuoteStatus = quote.Status;

                    if (orderByQuoteId.TryGetValue(quote.Id, out var order))
                    {
                        row.OrderId = order.Id;
                        row.OrderNumber = order.OrderNumber;
                        row.OrderStatus = order.Status;

                        var orderMilestones = milestonesByOrderId[order.Id].ToList();
                        row.HasDepositInvoice = orderMilestones.Any(x => x.Kind == OrderPaymentMilestoneKind.Deposit && x.IsInvoiced)
                            || invoicesByOrderId[order.Id].Any();

                        var orderJobs = jobsByOrderId[order.Id].ToList();
                        row.HasInstallationJob = orderJobs.Count > 0;
                        row.InstallationCompleted = orderJobs.Count > 0 && orderJobs.All(x => x.Status == FieldServiceJobStatus.Completed);

                        row.HasFinalInvoice = orderMilestones.Any(x => x.Kind == OrderPaymentMilestoneKind.Final && x.IsInvoiced)
                            || (order.PaymentTerms != PaymentTerms.Milestone && invoicesByOrderId[order.Id].Any());
                    }
                }
            }

            (row.CurrentStage, row.PendingAction) = DescribeStage(row);
            rows.Add(row);
        }

        return rows;
    }

    private static (string CurrentStage, string? PendingAction) DescribeStage(WorkflowMonitorRowDto row)
    {
        if (row.HasFinalInvoice)
        {
            return ("Final Invoice Issued", null);
        }

        if (row.HasInstallationJob)
        {
            return row.InstallationCompleted
                ? ("Installation Completed", "Ready for final invoice")
                : ("Installation Scheduled", "Awaiting installation completion");
        }

        if (row.OrderId.HasValue)
        {
            if (row.HasDepositInvoice)
            {
                return ("Deposit Invoice Issued", "Schedule installation");
            }

            return row.OrderStatus == OrderStatus.Confirmed
                ? ("Order Confirmed", "Awaiting deposit invoice")
                : ("Order Submitted", "Awaiting confirmation");
        }

        if (row.QuoteId.HasValue)
        {
            return row.QuoteStatus switch
            {
                QuoteStatus.Accepted => ("Quote Accepted", "Convert to order"),
                QuoteStatus.Sent => ("Quote Sent", "Awaiting customer decision"),
                QuoteStatus.Rejected => ("Quote Rejected", null),
                QuoteStatus.Expired => ("Quote Expired", null),
                _ => ("Quote Drafted", "Send to customer")
            };
        }

        if (row.ProposalId.HasValue)
        {
            return row.ProposalStatus switch
            {
                ProposalStatus.Accepted => ("Proposal Approved", "Convert to quote"),
                ProposalStatus.Sent => ("Proposal Sent", "Awaiting customer decision"),
                ProposalStatus.Rejected => ("Proposal Rejected", null),
                _ => ("Proposal Drafted", "Send to customer")
            };
        }

        return row.OpportunityStatus switch
        {
            OpportunityStatus.Won => ("Closed Won", null),
            OpportunityStatus.Lost => ("Closed Lost", null),
            _ => ("Opportunity Opened", "Conduct needs assessment / draft proposal")
        };
    }
}
