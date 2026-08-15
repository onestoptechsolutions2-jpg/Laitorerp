using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Volo.Abp;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the margin gate built 2026-08-15 in response to the agentic-AI narrative comparison: a
// Quote can't cross Draft -> Sent while its computed margin sits below the configured floor
// (ErpSettings.SalesMarginFloorPercent, default 15%) without an explicit override reason. Note:
// ErpTestBase swaps in AlwaysAllowAuthorizationService, so every permission check always passes
// here - these tests cover the margin-threshold and reason-required logic, not the
// Sales.OverrideMarginGate permission denial itself (same documented limitation as
// DeletionGate's approval-filing branch).
public class QuoteMarginGateTests : ErpTestBase
{
    private async Task<(QuoteAppService QuoteAppService, QuoteLineAppService LineAppService, Guid CustomerId)> SeedAsync()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Margin Test Ltd" });

        return (
            GetRequiredService<QuoteAppService>(),
            GetRequiredService<QuoteLineAppService>(),
            customer.Id
        );
    }

    private static CreateUpdateQuoteDto ToSentUpdate(QuoteDto quote, string? overrideReason = null)
    {
        return new CreateUpdateQuoteDto
        {
            CustomerId = quote.CustomerId,
            Title = quote.Title,
            Status = QuoteStatus.Sent,
            IssueDate = quote.IssueDate,
            CurrencyCode = quote.CurrencyCode,
            MarginOverrideReason = overrideReason
        };
    }

    [Fact]
    public async Task Sending_A_BelowFloor_Quote_Without_Override_Throws()
    {
        var (quoteAppService, lineAppService, customerId) = await SeedAsync();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customerId,
            Title = "Thin Margin Deal",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        // UnitPrice 100, Cost 92 -> 8% margin, below the 15% default floor.
        await lineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            Description = "Router",
            UnitPrice = 100m,
            Quantity = 1,
            Cost = 92m
        });

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => quoteAppService.UpdateAsync(quote.Id, ToSentUpdate(quote)));
    }

    [Fact]
    public async Task Sending_A_BelowFloor_Quote_With_Override_Reason_Succeeds_And_Stamps_Audit_Fields()
    {
        var (quoteAppService, lineAppService, customerId) = await SeedAsync();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customerId,
            Title = "Strategic Loss Leader",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        await lineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            Description = "Router",
            UnitPrice = 100m,
            Quantity = 1,
            Cost = 92m
        });

        var updated = await quoteAppService.UpdateAsync(quote.Id, ToSentUpdate(quote, "Founder approved - strategic account"));

        Assert.Equal(QuoteStatus.Sent, updated.Status);
        Assert.Equal("Founder approved - strategic account", updated.MarginOverrideReason);
        // MarginOverrideByUserId isn't asserted here - the bare test host has no authenticated
        // CurrentUser, so it's legitimately null in this context (same documented limitation as
        // ChangeRequestAppServiceTests.ApprovedByUserId).
        Assert.NotNull(updated.MarginOverrideAt);
    }

    [Fact]
    public async Task Sending_An_AtOrAboveFloor_Quote_Succeeds_Without_Override_Reason()
    {
        var (quoteAppService, lineAppService, customerId) = await SeedAsync();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customerId,
            Title = "Healthy Margin Deal",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        // UnitPrice 100, Cost 50 -> 50% margin, comfortably above the 15% default floor.
        await lineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            Description = "Access Point",
            UnitPrice = 100m,
            Quantity = 1,
            Cost = 50m
        });

        var updated = await quoteAppService.UpdateAsync(quote.Id, ToSentUpdate(quote));

        Assert.Equal(QuoteStatus.Sent, updated.Status);
        Assert.Null(updated.MarginOverrideReason);
    }

    [Fact]
    public async Task Sending_A_Quote_With_No_Lines_Is_Never_Blocked()
    {
        var (quoteAppService, _, customerId) = await SeedAsync();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customerId,
            Title = "Placeholder Quote",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var updated = await quoteAppService.UpdateAsync(quote.Id, ToSentUpdate(quote));

        Assert.Equal(QuoteStatus.Sent, updated.Status);
    }
}
