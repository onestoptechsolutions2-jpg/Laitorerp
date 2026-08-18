using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Dtos.Workspace;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Workspace;

// Same aggregation-service shape as DashboardAppService, just filtered to CurrentUser.Id instead
// of the whole org - the personal "what's on my plate" view the 2026-07-19 audit flagged as
// missing. Problem isn't included: unlike Ticket/FieldServiceJob, Problem has no assignee field
// (see Entities/Support/Problem.cs), so there's no "mine" to filter it by.
public class MyWorkspaceAppService : ApplicationService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IRepository<ChangeRequest, Guid> _changeRequestRepository;
    private readonly IRepository<EscalationItem, Guid> _escalationItemRepository;
    private readonly IRepository<CustomerTask, Guid> _customerTaskRepository;
    private readonly IRepository<ProjectTask, Guid> _projectTaskRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<OrderPaymentMilestone, Guid> _milestoneRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly EscalationItemAppService _escalationItemAppService;
    private readonly IFeatureChecker _featureChecker;
    private readonly ISettingProvider _settingProvider;
    private readonly IClock _clock;

    public MyWorkspaceAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IRepository<ChangeRequest, Guid> changeRequestRepository,
        IRepository<EscalationItem, Guid> escalationItemRepository,
        IRepository<CustomerTask, Guid> customerTaskRepository,
        IRepository<ProjectTask, Guid> projectTaskRepository,
        IRepository<Project, Guid> projectRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<OrderPaymentMilestone, Guid> milestoneRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        EscalationItemAppService escalationItemAppService,
        IFeatureChecker featureChecker,
        ISettingProvider settingProvider,
        IClock clock)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _jobRepository = jobRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _changeRequestRepository = changeRequestRepository;
        _escalationItemRepository = escalationItemRepository;
        _customerTaskRepository = customerTaskRepository;
        _projectTaskRepository = projectTaskRepository;
        _projectRepository = projectRepository;
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _milestoneRepository = milestoneRepository;
        _invoiceRepository = invoiceRepository;
        _escalationItemAppService = escalationItemAppService;
        _featureChecker = featureChecker;
        _settingProvider = settingProvider;
        _clock = clock;
    }

    public async Task<MyWorkspaceDto> GetAsync()
    {
        var dto = new MyWorkspaceDto();

        if (!CurrentUser.Id.HasValue)
        {
            return dto;
        }

        var userId = CurrentUser.Id.Value;

        var tickets = await _ticketRepository.GetListAsync(x =>
            x.AssignedToUserId == userId && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);

        var jobs = await _jobRepository.GetListAsync(x =>
            x.AssignedToUserId == userId &&
            (x.Status == FieldServiceJobStatus.Scheduled || x.Status == FieldServiceJobStatus.InProgress));

        var customerIds = tickets.Select(x => x.CustomerId)
            .Concat(jobs.Select(x => x.CustomerId))
            .Distinct()
            .ToList();
        var namesById = customerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);

        dto.Tickets = tickets
            .OrderBy(x => x.SlaDueDate ?? DateTime.MaxValue)
            .Select(x => new MyTicketDto
            {
                Id = x.Id,
                TicketNumber = x.TicketNumber,
                Subject = x.Subject,
                CustomerName = namesById.GetValueOrDefault(x.CustomerId, string.Empty),
                Status = x.Status,
                Priority = x.Priority,
                SlaDueDate = x.SlaDueDate
            })
            .ToList();

        dto.Jobs = jobs
            .OrderBy(x => x.ScheduledDate)
            .Select(x => new MyJobDto
            {
                Id = x.Id,
                CustomerName = namesById.GetValueOrDefault(x.CustomerId, string.Empty),
                Type = x.Type,
                Status = x.Status,
                ScheduledDate = x.ScheduledDate
            })
            .ToList();

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide))
        {
            var pending = await _deletionRequestRepository.GetListAsync(x => x.Status == DeletionRequestStatus.Pending);
            dto.PendingDeletionRequestCount = pending.Count;
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Changes.Approve))
        {
            var pendingChanges = await _changeRequestRepository.GetListAsync(x => x.Status == ChangeRequestStatus.PendingApproval);
            dto.PendingChangeRequestCount = pendingChanges.Count;
        }

        // Unlike the two counts above (gated on one fixed permission), an EscalationItem's
        // RequiredPermission varies per row - so "how many can I actually decide" needs a
        // per-distinct-permission check rather than one flat count, reusing the exact same
        // CanDecideAsync logic EscalationItemAppService.ApproveAsync/RejectAsync enforce (row
        // permission OR the Escalations.Decide catch-all) so this count never overclaims what
        // the Escalations page itself will actually let the user act on.
        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Escalations.Default))
        {
            var pendingEscalations = await _escalationItemRepository.GetListAsync(x => x.Status == EscalationItemStatus.Pending);
            var decidableCount = 0;
            foreach (var group in pendingEscalations.GroupBy(x => x.RequiredPermission))
            {
                if (await _escalationItemAppService.CanDecideAsync(group.Key))
                {
                    decidableCount += group.Count();
                }
            }
            dto.PendingEscalationCount = decidableCount;
        }

        await LoadRemindersAsync(dto, userId);
        await LoadOrdersReadyToInvoiceAsync(dto, userId);

        return dto;
    }

    // Mirrors the exact condition Pages/Sales/Orders/Detail.cshtml.cs computes for its own
    // "Issue Final Invoice" button (CanIssueFinalInvoice) - the 2026-08-18 consolidation audit
    // flagged that a completed job never actively notifies anyone, it only silently makes that
    // button appear next time someone happens to revisit the Order page. This surfaces the same
    // condition proactively to the order's own salesperson instead. No new entity/trigger needed -
    // this is a read, not a write; the actual invoicing action is still the existing button.
    private async Task LoadOrdersReadyToInvoiceAsync(MyWorkspaceDto dto, Guid userId)
    {
        var candidateOrders = await _orderRepository.GetListAsync(x =>
            x.SalespersonUserId == userId && (x.Status == OrderStatus.Confirmed || x.Status == OrderStatus.Fulfilled));

        if (candidateOrders.Count == 0)
        {
            return;
        }

        var orderIds = candidateOrders.Select(x => x.Id).ToList();

        var invoicedOrderIds = (await _invoiceRepository.GetListAsync(x => x.OrderId.HasValue && orderIds.Contains(x.OrderId.Value)))
            .Select(x => x.OrderId!.Value)
            .ToHashSet();

        var milestoneOrderIds = candidateOrders.Where(x => x.PaymentTerms == PaymentTerms.Milestone).Select(x => x.Id).ToList();
        var finalInvoicedMilestoneOrderIds = milestoneOrderIds.Count == 0
            ? new HashSet<Guid>()
            : (await _milestoneRepository.GetListAsync(x =>
                    milestoneOrderIds.Contains(x.OrderId) && x.Kind == OrderPaymentMilestoneKind.Final && x.IsInvoiced))
                .Select(x => x.OrderId)
                .ToHashSet();

        var jobs = await _jobRepository.GetListAsync(x => x.OrderId != null && orderIds.Contains(x.OrderId.Value));
        var jobsByOrderId = jobs.GroupBy(x => x.OrderId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var readyOrders = candidateOrders.Where(order =>
        {
            var hasFinalInvoice = order.PaymentTerms == PaymentTerms.Milestone
                ? finalInvoicedMilestoneOrderIds.Contains(order.Id)
                : invoicedOrderIds.Contains(order.Id);
            if (hasFinalInvoice)
            {
                return false;
            }

            var orderJobs = jobsByOrderId.GetValueOrDefault(order.Id, new List<FieldServiceJob>());
            return !orderJobs.Any(x => x.Status != FieldServiceJobStatus.Completed);
        }).ToList();

        if (readyOrders.Count == 0)
        {
            return;
        }

        var customerIds = readyOrders.Select(x => x.CustomerId).Distinct().ToList();
        var customerNamesById = (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.Name);

        var readyOrderIds = readyOrders.Select(x => x.Id).ToList();
        var linesByOrderId = (await _orderLineRepository.GetListAsync(x => readyOrderIds.Contains(x.OrderId)))
            .GroupBy(x => x.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Total()));

        dto.OrdersReadyToInvoice = readyOrders
            .OrderBy(x => x.OrderNumber)
            .Select(x => new MyOrderReadyToInvoiceDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = customerNamesById.GetValueOrDefault(x.CustomerId, string.Empty),
                Total = linesByOrderId.GetValueOrDefault(x.Id, 0m)
            })
            .ToList();
    }

    // Passive, on-screen-only reminders - no worker/email, computed fresh on every request, same
    // convention DashboardAppService.GetSalesStatsAsync uses for OverdueInvoices. CustomerTask
    // always contributes; ProjectTask only when ProjectManagement is enabled, since the entity is
    // meaningless with the module off (mirrors every other optional-module read in this app).
    private async Task LoadRemindersAsync(MyWorkspaceDto dto, Guid userId)
    {
        var now = _clock.Now;
        var dueSoonLeadDays = int.TryParse(await _settingProvider.GetOrNullAsync(ErpSettings.TaskDueSoonLeadDays), out var parsed) ? parsed : 3;

        var reminders = new List<MyReminderDto>();

        var customerTasks = await _customerTaskRepository.GetListAsync(x =>
            x.AssignedToUserId == userId && !x.IsCompleted);

        var customerIds = customerTasks.Select(x => x.CustomerId).Distinct().ToList();
        var customerNamesById = customerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);

        reminders.AddRange(customerTasks.Select(x => new MyReminderDto
        {
            Id = x.Id,
            EntityType = "CustomerTask",
            ParentId = x.CustomerId,
            ParentName = customerNamesById.GetValueOrDefault(x.CustomerId, string.Empty),
            Title = x.Title,
            DueDate = x.DueDate,
            IsOverdue = TaskDueStatus.IsOverdue(x.DueDate, x.IsCompleted, now),
            IsDueSoon = TaskDueStatus.IsDueSoon(x.DueDate, x.IsCompleted, now, dueSoonLeadDays)
        }));

        if (await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement))
        {
            var projectTasks = await _projectTaskRepository.GetListAsync(x =>
                x.AssignedToUserId == userId && !x.IsCompleted);

            var projectIds = projectTasks.Select(x => x.ProjectId).Distinct().ToList();
            var projectNamesById = projectIds.Count == 0
                ? new Dictionary<Guid, string>()
                : (await _projectRepository.GetListAsync(x => projectIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Title);

            reminders.AddRange(projectTasks.Select(x => new MyReminderDto
            {
                Id = x.Id,
                EntityType = "ProjectTask",
                ParentId = x.ProjectId,
                ParentName = projectNamesById.GetValueOrDefault(x.ProjectId, string.Empty),
                Title = x.Title,
                DueDate = x.DueDate,
                IsOverdue = TaskDueStatus.IsOverdue(x.DueDate, x.IsCompleted, now),
                IsDueSoon = TaskDueStatus.IsDueSoon(x.DueDate, x.IsCompleted, now, dueSoonLeadDays)
            }));
        }

        dto.Reminders = reminders.OrderBy(x => x.DueDate ?? DateTime.MaxValue).ToList();
    }
}
