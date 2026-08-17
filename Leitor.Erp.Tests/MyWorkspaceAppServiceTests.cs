using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Support;
using Leitor.Erp.Services.Workspace;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Security.Claims;
using Xunit;

namespace Leitor.Erp.Tests;

// MyWorkspaceAppService.GetAsync() short-circuits to an empty DTO whenever CurrentUser.Id is
// unset (see its own null-CurrentUser guard) - the bare test host has no authenticated principal
// by default, same gap ChangeRequestAppServiceTests' own comment notes for ApprovedByUserId. Every
// test here impersonates a user via ICurrentPrincipalAccessor.Change so GetAsync() actually
// exercises its filtering/counting logic instead of returning early.
public class MyWorkspaceAppServiceTests : ErpTestBase
{
    private IDisposable ImpersonateAsUser(Guid userId)
    {
        var principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString())
        }));
        return principalAccessor.Change(principal);
    }

    [Fact]
    public async Task GetAsync_Counts_Only_PendingApproval_ChangeRequests()
    {
        await EnsureDatabaseCreatedAsync();

        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.AssetManagement, "true", "T", null);
        await featureManager.SetAsync(ErpFeatures.ChangeEnablement, "true", "T", null);

        var ciAppService = GetRequiredService<ConfigurationItemAppService>();
        var ci = await ciAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Client Firewall" });

        var changeAppService = GetRequiredService<ChangeRequestAppService>();
        await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ci.Id,
            Title = "Reconfigure client VPN",
            Tier = ChangeTier.Normal // reaches PendingApproval
        });
        await changeAppService.CreateAsync(new CreateUpdateChangeRequestDto
        {
            ConfigurationItemId = ci.Id,
            Title = "Routine firmware update",
            Tier = ChangeTier.Standard // auto-approved, should not be counted
        });

        using (ImpersonateAsUser(Guid.NewGuid()))
        {
            var workspaceAppService = GetRequiredService<MyWorkspaceAppService>();
            var workspace = await workspaceAppService.GetAsync();

            Assert.Equal(1, workspace.PendingChangeRequestCount);
        }
    }

    [Fact]
    public async Task GetAsync_Only_Returns_Tickets_And_Jobs_Assigned_To_The_Current_User()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Globex Corp" });

        var myUserId = Guid.NewGuid();
        var someoneElsesUserId = Guid.NewGuid();

        var ticketAppService = GetRequiredService<TicketAppService>();
        var myOpenTicket = await ticketAppService.CreateAsync(new CreateUpdateTicketDto
        {
            CustomerId = customer.Id,
            Subject = "VPN keeps dropping",
            Status = TicketStatus.Open,
            AssignedToUserId = myUserId
        });
        await ticketAppService.CreateAsync(new CreateUpdateTicketDto
        {
            CustomerId = customer.Id,
            Subject = "Someone else's ticket",
            Status = TicketStatus.Open,
            AssignedToUserId = someoneElsesUserId
        });
        var myClosedTicket = await ticketAppService.CreateAsync(new CreateUpdateTicketDto
        {
            CustomerId = customer.Id,
            Subject = "Already resolved",
            Status = TicketStatus.Open,
            AssignedToUserId = myUserId
        });
        await ticketAppService.UpdateAsync(myClosedTicket.Id, new CreateUpdateTicketDto
        {
            CustomerId = customer.Id,
            Subject = myClosedTicket.Subject,
            Status = TicketStatus.Closed,
            AssignedToUserId = myUserId
        });

        var jobAppService = GetRequiredService<FieldServiceJobAppService>();
        var myScheduledJob = await jobAppService.CreateAsync(new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Type = FieldServiceJobType.Installation,
            Status = FieldServiceJobStatus.Scheduled,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            AssignedToUserId = myUserId
        });
        await jobAppService.CreateAsync(new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Type = FieldServiceJobType.Installation,
            Status = FieldServiceJobStatus.Scheduled,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            AssignedToUserId = someoneElsesUserId
        });

        using (ImpersonateAsUser(myUserId))
        {
            var workspaceAppService = GetRequiredService<MyWorkspaceAppService>();
            var workspace = await workspaceAppService.GetAsync();

            var ticket = Assert.Single(workspace.Tickets);
            Assert.Equal(myOpenTicket.Id, ticket.Id);

            var job = Assert.Single(workspace.Jobs);
            Assert.Equal(myScheduledJob.Id, job.Id);
        }
    }

    [Fact]
    public async Task GetAsync_Counts_Only_Pending_Escalations_Decidable_By_The_Current_User()
    {
        await EnsureDatabaseCreatedAsync();

        var escalationRepository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var pending = new EscalationItem(
            Guid.NewGuid(), "Quote.MarginOverride", "Quote", Guid.NewGuid(),
            ErpPermissions.Sales.OverrideMarginGate, null, null, DateTime.UtcNow, "pending one");
        await escalationRepository.InsertAsync(pending, autoSave: true);

        var alreadyDecided = new EscalationItem(
            Guid.NewGuid(), "Quote.MarginOverride", "Quote", Guid.NewGuid(),
            ErpPermissions.Sales.OverrideMarginGate, null, null, DateTime.UtcNow, "already decided");
        alreadyDecided.Status = EscalationItemStatus.Approved;
        await escalationRepository.InsertAsync(alreadyDecided, autoSave: true);

        using (ImpersonateAsUser(Guid.NewGuid()))
        {
            var workspaceAppService = GetRequiredService<MyWorkspaceAppService>();
            var workspace = await workspaceAppService.GetAsync();

            // AlwaysAllowAuthorizationService grants every permission check in this test host, so
            // this covers the Pending-vs-already-decided filtering/grouping logic itself, not the
            // per-row RequiredPermission gate - see GlobalSearchAppServiceTests' own comment for
            // the same standing limitation.
            Assert.Equal(1, workspace.PendingEscalationCount);
        }
    }
}
