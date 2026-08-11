using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the gap found 2026-08-11: LeadAppService never overrode DeleteAsync, so deleting a Lead
// left its LeadTouch (outreach/channel-activity) history orphaned in the database instead of
// cascading, unlike every other module with dependent child records. The deletion-approval gate
// itself (DeletionGate.EnsureImmediateDeleteAllowedAsync) isn't separately testable here - this
// suite's AlwaysAllowAuthorizationService always grants DeletionApprovals.Decide, so every delete
// takes the immediate-delete path, same constraint every other *AppServiceTests.cs in this project
// already lives with.
public class LeadDeletionTests : ErpTestBase
{
    [Fact]
    public async Task DeleteAsync_Cascades_To_LeadTouch_Rows()
    {
        await EnsureDatabaseCreatedAsync();

        var leadAppService = GetRequiredService<LeadAppService>();
        var leadTouchAppService = GetRequiredService<LeadTouchAppService>();
        var leadTouchRepository = GetRequiredService<IRepository<LeadTouch, Guid>>();

        var lead = await leadAppService.CreateAsync(new CreateUpdateLeadDto { Name = "Mnyanga" });
        await leadTouchAppService.CreateAsync(new CreateLeadTouchDto
        {
            LeadId = lead.Id,
            Channel = LeadChannel.WhatsApp,
            Direction = LeadDirection.Outbound
        });

        var touchesBeforeDelete = await leadTouchRepository.GetListAsync(x => x.LeadId == lead.Id);
        Assert.Single(touchesBeforeDelete);

        await leadAppService.DeleteAsync(lead.Id);

        var touchesAfterDelete = await leadTouchRepository.GetListAsync(x => x.LeadId == lead.Id);
        Assert.Empty(touchesAfterDelete);
    }
}
