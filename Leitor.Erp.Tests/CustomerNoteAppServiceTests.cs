using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp.Timing;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 communication engagement tracking extension to CustomerNote: Direction
// (reusing LeadDirection) and TouchedAt, mirroring LeadTouch's own shape/reasoning for Customers.
public class CustomerNoteAppServiceTests : ErpTestBase
{
    private async Task<Guid> CreateCustomerAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Engagement Test Co" });
        return customer.Id;
    }

    [Fact]
    public async Task CreateAsync_With_No_TouchedAt_Defaults_To_Now()
    {
        var customerId = await CreateCustomerAsync();
        var customerNoteAppService = GetRequiredService<CustomerNoteAppService>();
        var clock = GetRequiredService<IClock>();

        // Compare against IClock.Now (what CustomerNoteAppService actually stamps TouchedAt with),
        // not DateTime.UtcNow - ABP's clock can be configured to a non-UTC Kind, and DateTime
        // comparisons ignore Kind entirely, so mixing the two sources here produces a flaky
        // assertion even though the app's own clock is internally consistent.
        var before = clock.Now;
        var note = await customerNoteAppService.CreateAsync(new CreateCustomerNoteDto
        {
            CustomerId = customerId,
            Type = CustomerNoteType.Call,
            Text = "Discussed renewal timeline"
        });
        var after = clock.Now;

        Assert.InRange(note.TouchedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_With_Explicit_Past_TouchedAt_Is_Preserved()
    {
        var customerId = await CreateCustomerAsync();
        var customerNoteAppService = GetRequiredService<CustomerNoteAppService>();

        var backfilledTime = DateTime.UtcNow.AddDays(-3);
        var note = await customerNoteAppService.CreateAsync(new CreateCustomerNoteDto
        {
            CustomerId = customerId,
            Type = CustomerNoteType.MeetingOrVisit,
            Text = "Backfilled: on-site visit last week",
            TouchedAt = backfilledTime
        });

        Assert.Equal(backfilledTime, note.TouchedAt);
    }

    [Fact]
    public async Task GetListAsync_Defaults_To_TouchedAt_Descending()
    {
        var customerId = await CreateCustomerAsync();
        var customerNoteAppService = GetRequiredService<CustomerNoteAppService>();

        await customerNoteAppService.CreateAsync(new CreateCustomerNoteDto
        {
            CustomerId = customerId,
            Text = "Older touch",
            TouchedAt = DateTime.UtcNow.AddDays(-5)
        });
        await customerNoteAppService.CreateAsync(new CreateCustomerNoteDto
        {
            CustomerId = customerId,
            Text = "Newer touch",
            TouchedAt = DateTime.UtcNow.AddDays(-1)
        });

        var result = await customerNoteAppService.GetListAsync(new GetCustomerNoteListInput { CustomerId = customerId });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Newer touch", result.Items[0].Text);
        Assert.Equal("Older touch", result.Items[1].Text);
    }

    [Fact]
    public async Task Direction_Round_Trips_Through_The_Dto()
    {
        var customerId = await CreateCustomerAsync();
        var customerNoteAppService = GetRequiredService<CustomerNoteAppService>();

        var inbound = await customerNoteAppService.CreateAsync(new CreateCustomerNoteDto
        {
            CustomerId = customerId,
            Text = "Customer called in",
            Direction = LeadDirection.Inbound
        });

        var reloaded = await customerNoteAppService.GetAsync(inbound.Id);
        Assert.Equal(LeadDirection.Inbound, reloaded.Direction);
    }
}
