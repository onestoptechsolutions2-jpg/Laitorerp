using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Calendar;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Calendar;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Calendar;

// Standalone CalendarEvent CRUD, plus GetFeedAsync which merges those editable rows with a
// read-only projection of every other module's own dated records - the calendar never becomes a
// second source of truth for FieldServiceJob/Ticket/ProjectTask/CustomerTask, it only displays
// them. Each read-only section is gated on that module's own view permission (and feature, where
// optional), same convention DashboardAppService.GetAsync uses per section - a user never sees a
// feed item they couldn't already reach by navigating to that module directly.
[RequiresFeature(ErpFeatures.Calendar)]
public class CalendarEventAppService :
    CrudAppService<CalendarEvent, CalendarEventDto, Guid, GetCalendarEventListInput, CreateUpdateCalendarEventDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<ProjectTask, Guid> _projectTaskRepository;
    private readonly IRepository<CustomerTask, Guid> _customerTaskRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IFeatureChecker _featureChecker;

    public CalendarEventAppService(
        IRepository<CalendarEvent, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Agent, Guid> agentRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<ProjectTask, Guid> projectTaskRepository,
        IRepository<CustomerTask, Guid> customerTaskRepository,
        IRepository<Customer, Guid> customerRepository,
        IFeatureChecker featureChecker)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;
        _agentRepository = agentRepository;
        _jobRepository = jobRepository;
        _ticketRepository = ticketRepository;
        _projectTaskRepository = projectTaskRepository;
        _customerTaskRepository = customerTaskRepository;
        _customerRepository = customerRepository;
        _featureChecker = featureChecker;

        GetPolicyName = ErpPermissions.Calendar.Default;
        GetListPolicyName = ErpPermissions.Calendar.Default;
        CreatePolicyName = ErpPermissions.Calendar.Create;
        UpdatePolicyName = ErpPermissions.Calendar.Edit;
        DeletePolicyName = ErpPermissions.Calendar.Delete;
    }

    protected override async Task<IQueryable<CalendarEvent>> CreateFilteredQueryAsync(GetCalendarEventListInput input)
    {
        input.Sorting ??= $"{nameof(CalendarEvent.StartDate)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.From.HasValue, x => x.EndDate == null ? x.StartDate >= input.From!.Value : x.EndDate >= input.From!.Value)
            .WhereIf(input.To.HasValue, x => x.StartDate <= input.To!.Value);
    }

    public override async Task<PagedResultDto<CalendarEventDto>> GetListAsync(GetCalendarEventListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveNamesAsync(IReadOnlyCollection<CalendarEventDto> events)
    {
        var userIds = events.Where(x => x.AssignedToUserId.HasValue).Select(x => x.AssignedToUserId!.Value).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var namesById = (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName);
            foreach (var e in events)
            {
                if (e.AssignedToUserId.HasValue && namesById.TryGetValue(e.AssignedToUserId.Value, out var userName))
                {
                    e.AssignedToUserName = userName;
                }
            }
        }

        var agentIds = events.Where(x => x.AgentId.HasValue).Select(x => x.AgentId!.Value).Distinct().ToList();
        if (agentIds.Count > 0)
        {
            var namesById = (await _agentRepository.GetListAsync(x => agentIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);
            foreach (var e in events)
            {
                if (e.AgentId.HasValue && namesById.TryGetValue(e.AgentId.Value, out var agentName))
                {
                    e.AgentName = agentName;
                }
            }
        }
    }

    protected override Task<CalendarEvent> MapToEntityAsync(CreateUpdateCalendarEventDto createInput)
    {
        var entity = new CalendarEvent(GuidGenerator.Create(), createInput.Title, createInput.StartDate);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateCalendarEventDto updateInput, CalendarEvent entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateCalendarEventDto input, CalendarEvent entity)
    {
        entity.Title = input.Title;
        entity.Description = input.Description;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.AssignedToUserId = input.AssignedToUserId;
        entity.AgentId = input.AgentId;
    }

    // Drag/resize on the calendar - deliberately narrower than UpdateAsync (only the fields a drag
    // gesture can actually change), same reasoning CustomerTaskAppService.OnPostToggleTaskAsync's
    // narrow field list has for its own single-purpose action.
    public async Task MoveAsync(Guid id, DateTime start, DateTime? end)
    {
        await CheckUpdatePolicyAsync();
        var entity = await Repository.GetAsync(id);
        entity.StartDate = start;
        entity.EndDate = end;
        await Repository.UpdateAsync(entity);
    }

    public async Task<List<CalendarFeedItemDto>> GetFeedAsync(DateTime from, DateTime to)
    {
        var feed = new List<CalendarFeedItemDto>();

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Calendar.Default))
        {
            var events = await Repository.GetListAsync(x =>
                (x.EndDate == null ? x.StartDate >= from : x.EndDate >= from) && x.StartDate <= to);
            var canEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Calendar.Edit);

            var userIds = events.Where(x => x.AssignedToUserId.HasValue).Select(x => x.AssignedToUserId!.Value).Distinct().ToList();
            var userNamesById = userIds.Count == 0
                ? new Dictionary<Guid, string>()
                : (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName);

            feed.AddRange(events.Select(x => new CalendarFeedItemDto
            {
                Id = x.Id,
                SourceType = "CalendarEvent",
                SourceId = x.Id,
                Title = x.Title,
                Start = x.StartDate,
                End = x.EndDate,
                AssignedToUserId = x.AssignedToUserId,
                AssignedToUserName = x.AssignedToUserId.HasValue ? userNamesById.GetValueOrDefault(x.AssignedToUserId.Value) : null,
                IsEditable = canEdit,
                Url = string.Empty
            }));
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Default))
        {
            var jobs = await _jobRepository.GetListAsync(x => x.ScheduledDate >= from && x.ScheduledDate <= to);
            var customerIds = jobs.Select(x => x.CustomerId).Distinct().ToList();
            var customerNamesById = customerIds.Count == 0
                ? new Dictionary<Guid, string>()
                : (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);

            feed.AddRange(jobs.Select(x => new CalendarFeedItemDto
            {
                Id = x.Id,
                SourceType = "FieldServiceJob",
                SourceId = x.Id,
                Title = $"{x.Type} - {customerNamesById.GetValueOrDefault(x.CustomerId, string.Empty)}",
                Start = x.ScheduledDate,
                End = null,
                AssignedToUserId = x.AssignedToUserId,
                IsEditable = false,
                Url = $"/FieldService/Jobs/Detail/{x.Id}"
            }));
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Default))
        {
            var tickets = await _ticketRepository.GetListAsync(x => x.SlaDueDate != null && x.SlaDueDate >= from && x.SlaDueDate <= to);

            feed.AddRange(tickets.Select(x => new CalendarFeedItemDto
            {
                Id = x.Id,
                SourceType = "Ticket",
                SourceId = x.Id,
                Title = $"{x.TicketNumber} - {x.Subject}",
                Start = x.SlaDueDate!.Value,
                End = null,
                AssignedToUserId = x.AssignedToUserId,
                IsEditable = false,
                Url = $"/Support/Tickets/Detail/{x.Id}"
            }));
        }

        if (await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement) &&
            await AuthorizationService.IsGrantedAsync(ErpPermissions.Projects.Default))
        {
            var projectTasks = await _projectTaskRepository.GetListAsync(x =>
                !x.IsCompleted && x.DueDate != null && x.DueDate >= from && x.DueDate <= to);

            feed.AddRange(projectTasks.Select(x => new CalendarFeedItemDto
            {
                Id = x.Id,
                SourceType = "ProjectTask",
                SourceId = x.Id,
                Title = x.Title,
                Start = x.DueDate!.Value,
                End = null,
                AssignedToUserId = x.AssignedToUserId,
                IsEditable = false,
                Url = $"/Projects/Detail/{x.ProjectId}"
            }));
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Default))
        {
            var customerTasks = await _customerTaskRepository.GetListAsync(x =>
                !x.IsCompleted && x.DueDate != null && x.DueDate >= from && x.DueDate <= to);

            feed.AddRange(customerTasks.Select(x => new CalendarFeedItemDto
            {
                Id = x.Id,
                SourceType = "CustomerTask",
                SourceId = x.Id,
                Title = x.Title,
                Start = x.DueDate!.Value,
                End = null,
                AssignedToUserId = x.AssignedToUserId,
                IsEditable = false,
                Url = $"/Customers/Detail/{x.CustomerId}"
            }));
        }

        return feed;
    }
}
