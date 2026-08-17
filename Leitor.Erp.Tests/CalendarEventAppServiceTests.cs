using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Calendar;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Calendar;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Projects;
using Leitor.Erp.Services.Support;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 Shared Team Calendar (Phase 3): CalendarEvent CRUD plus GetFeedAsync's
// merge of standalone events with read-only projections of FieldServiceJob/Ticket/ProjectTask.
// Note: AlwaysAllowAuthorizationService in ErpTestBase means cross-module permission-filtering on
// the feed (e.g. "a caller who can't view Tickets shouldn't see Ticket feed items") can't be fully
// exercised in this harness - same documented limitation GlobalSearchAppServiceTests/
// QuoteMarginGateTests already carry. These tests cover the query/merge/feature-gate logic itself.
public class CalendarEventAppServiceTests : ErpTestBase
{
    private async Task EnableCalendarAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.Calendar, "true", "T", null);
    }

    [Fact]
    public async Task CreateAsync_Then_UpdateAsync_Then_DeleteAsync_Round_Trips_A_Standalone_Event()
    {
        await EnableCalendarAsync();
        var calendarEventAppService = GetRequiredService<CalendarEventAppService>();

        var created = await calendarEventAppService.CreateAsync(new CreateUpdateCalendarEventDto
        {
            Title = "Team standup",
            StartDate = new DateTime(2026, 9, 1, 9, 0, 0)
        });
        Assert.Equal("Team standup", created.Title);

        var updated = await calendarEventAppService.UpdateAsync(created.Id, new CreateUpdateCalendarEventDto
        {
            Title = "Team standup (moved)",
            StartDate = new DateTime(2026, 9, 1, 10, 0, 0)
        });
        Assert.Equal("Team standup (moved)", updated.Title);

        await calendarEventAppService.DeleteAsync(created.Id);
        var remaining = await calendarEventAppService.GetListAsync(new GetCalendarEventListInput());
        Assert.Empty(remaining.Items);
    }

    [Fact]
    public async Task MoveAsync_Updates_StartDate_And_EndDate()
    {
        await EnableCalendarAsync();
        var calendarEventAppService = GetRequiredService<CalendarEventAppService>();

        var created = await calendarEventAppService.CreateAsync(new CreateUpdateCalendarEventDto
        {
            Title = "Site visit",
            StartDate = new DateTime(2026, 9, 5, 8, 0, 0)
        });

        var newStart = new DateTime(2026, 9, 6, 8, 0, 0);
        var newEnd = new DateTime(2026, 9, 6, 10, 0, 0);
        await calendarEventAppService.MoveAsync(created.Id, newStart, newEnd);

        var reloaded = await calendarEventAppService.GetAsync(created.Id);
        Assert.Equal(newStart, reloaded.StartDate);
        Assert.Equal(newEnd, reloaded.EndDate);
    }

    [Fact]
    public async Task GetFeedAsync_Merges_CalendarEvent_FieldServiceJob_And_Ticket_Within_Range()
    {
        await EnableCalendarAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Feed Test Co" });

        var calendarEventAppService = GetRequiredService<CalendarEventAppService>();
        var calendarEvent = await calendarEventAppService.CreateAsync(new CreateUpdateCalendarEventDto
        {
            Title = "Kickoff meeting",
            StartDate = new DateTime(2026, 9, 10, 9, 0, 0)
        });

        var jobAppService = GetRequiredService<FieldServiceJobAppService>();
        var job = await jobAppService.CreateAsync(new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Type = FieldServiceJobType.Installation,
            ScheduledDate = new DateTime(2026, 9, 12, 8, 0, 0)
        });

        var ticketAppService = GetRequiredService<TicketAppService>();
        var ticket = await ticketAppService.CreateAsync(new CreateUpdateTicketDto
        {
            CustomerId = customer.Id,
            Subject = "Escalated outage",
            Priority = TicketPriority.Urgent
        });
        // SlaDueDate is set from Priority at creation - confirm it landed inside the query window
        // rather than asserting a specific offset, since the exact SLA-hours setting isn't this
        // test's concern.
        var ticketDto = await ticketAppService.GetAsync(ticket.Id);
        Assert.NotNull(ticketDto.SlaDueDate);

        // An event well outside the query window - must never appear in the feed.
        await calendarEventAppService.CreateAsync(new CreateUpdateCalendarEventDto
        {
            Title = "Next quarter's event",
            StartDate = new DateTime(2027, 1, 1, 9, 0, 0)
        });

        // Ticket.SlaDueDate is computed from CreationTime (i.e. "now", not the September 2026
        // dates picked for the other two records) - the window needs to bracket both.
        var from = DateTime.UtcNow.AddHours(-1);
        var to = new DateTime(2026, 9, 30);
        var feed = await calendarEventAppService.GetFeedAsync(from, to);

        Assert.Contains(feed, x => x.SourceType == "CalendarEvent" && x.SourceId == calendarEvent.Id);
        Assert.Contains(feed, x => x.SourceType == "FieldServiceJob" && x.SourceId == job.Id);
        Assert.Contains(feed, x => x.SourceType == "Ticket" && x.SourceId == ticket.Id);
        Assert.DoesNotContain(feed, x => x.Title == "Next quarter's event");

        var eventItem = feed.Single(x => x.SourceType == "CalendarEvent" && x.SourceId == calendarEvent.Id);
        Assert.True(eventItem.IsEditable);

        var jobItem = feed.Single(x => x.SourceType == "FieldServiceJob" && x.SourceId == job.Id);
        Assert.False(jobItem.IsEditable);
        Assert.Equal($"/FieldService/Jobs/Detail/{job.Id}", jobItem.Url);
    }

    [Fact]
    public async Task GetFeedAsync_Includes_ProjectTask_Only_When_ProjectManagement_Feature_Enabled()
    {
        await EnableCalendarAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Project Feed Co" });

        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "true", "T", null);

        var projectAppService = GetRequiredService<ProjectAppService>();
        var project = await projectAppService.CreateAsync(new CreateUpdateProjectDto { CustomerId = customer.Id, Title = "Fibre rollout" });

        var projectTaskAppService = GetRequiredService<ProjectTaskAppService>();
        var task = await projectTaskAppService.CreateAsync(new CreateUpdateProjectTaskDto
        {
            ProjectId = project.Id,
            Title = "Trench the route",
            DueDate = new DateTime(2026, 9, 15)
        });

        var calendarEventAppService = GetRequiredService<CalendarEventAppService>();
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 30);

        var feedWithProjectsOn = await calendarEventAppService.GetFeedAsync(from, to);
        Assert.Contains(feedWithProjectsOn, x => x.SourceType == "ProjectTask" && x.SourceId == task.Id);

        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "false", "T", null);
        var feedWithProjectsOff = await calendarEventAppService.GetFeedAsync(from, to);
        Assert.DoesNotContain(feedWithProjectsOff, x => x.SourceType == "ProjectTask");
    }
}
