using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Workspace;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

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

    public MyWorkspaceAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _jobRepository = jobRepository;
        _deletionRequestRepository = deletionRequestRepository;
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

        return dto;
    }
}
