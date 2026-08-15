using System;
using System.Text.Json;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Sales;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the generic escalation/approval queue built 2026-08-15 as the missing piece from the
// agentic-AI narrative review ([[project_agentic_ai_narrative_review_2026-08-15]]), with the
// margin gate wired as its first real consumer. Same AlwaysAllowAuthorizationService limitation as
// every other governance test in this suite: the FILING branch (QuoteAppService/OrderAppService
// calling EscalationGate.FileAsync when the current user lacks Sales.OverrideMarginGate) is
// untestable here since IsGrantedAsync always returns true - see QuoteMarginGateTests/
// OrderMarginGateTests. These tests instead seed an EscalationItem directly via the repository
// (bypassing the gate) to exercise EscalationItemAppService's approve/reject/dispatch logic in
// isolation, which doesn't depend on the filer's permission at all.
public class EscalationItemTests : ErpTestBase
{
    private async Task<Guid> SeedCustomerAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Escalation Test Ltd" });
        return customer.Id;
    }

    private async Task<Guid> SeedThinMarginQuoteAsync(Guid customerId)
    {
        var quoteAppService = GetRequiredService<QuoteAppService>();
        var lineAppService = GetRequiredService<QuoteLineAppService>();

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

        return quote.Id;
    }

    private async Task<Guid> SeedThinMarginOrderAsync(Guid customerId)
    {
        var orderAppService = GetRequiredService<OrderAppService>();
        var lineAppService = GetRequiredService<OrderLineAppService>();

        var order = await orderAppService.CreateAsync(new CreateUpdateOrderDto
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        await lineAppService.CreateAsync(new CreateUpdateOrderLineDto
        {
            OrderId = order.Id,
            Description = "Router",
            UnitPrice = 100m,
            Quantity = 1,
            Cost = 92m
        });

        return order.Id;
    }

    private async Task<EscalationItem> SeedEscalationAsync(string actionType, string entityType, Guid entityId, string reason)
    {
        var repository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var item = new EscalationItem(
            Guid.NewGuid(), actionType, entityType, entityId, ErpPermissions.Sales.OverrideMarginGate,
            JsonSerializer.Serialize(new MarginOverridePayload { OverrideReason = reason }),
            null, DateTime.UtcNow, reason);
        await repository.InsertAsync(item, autoSave: true);
        return item;
    }

    [Fact]
    public async Task Approving_A_Quote_MarginOverride_Escalation_Dispatches_To_Handler_And_Sends_The_Quote()
    {
        var customerId = await SeedCustomerAsync();
        var quoteId = await SeedThinMarginQuoteAsync(customerId);
        var item = await SeedEscalationAsync("Quote.MarginOverride", "Quote", quoteId, "Founder approved - strategic account");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await escalationAppService.ApproveAsync(item.Id);

        var quoteAppService = GetRequiredService<QuoteAppService>();
        var quote = await quoteAppService.GetAsync(quoteId);
        Assert.Equal(QuoteStatus.Sent, quote.Status);
        Assert.Equal("Founder approved - strategic account", quote.MarginOverrideReason);

        var repository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var reloaded = await repository.GetAsync(item.Id);
        Assert.Equal(EscalationItemStatus.Approved, reloaded.Status);
        Assert.NotNull(reloaded.DecidedAt);
    }

    [Fact]
    public async Task Approving_An_Order_MarginOverride_Escalation_Dispatches_To_Handler_And_Confirms_The_Order()
    {
        var customerId = await SeedCustomerAsync();
        var orderId = await SeedThinMarginOrderAsync(customerId);
        var item = await SeedEscalationAsync("Order.MarginOverride", "Order", orderId, "Founder approved - strategic account");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await escalationAppService.ApproveAsync(item.Id);

        var orderAppService = GetRequiredService<OrderAppService>();
        var order = await orderAppService.GetAsync(orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal("Founder approved - strategic account", order.MarginOverrideReason);
    }

    [Fact]
    public async Task Approve_When_No_Handler_Is_Registered_Throws_UserFriendlyException_And_Leaves_Item_Pending()
    {
        await EnsureDatabaseCreatedAsync();
        var item = await SeedEscalationAsync("Bogus.ActionType", "Quote", Guid.NewGuid(), "irrelevant");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await Assert.ThrowsAsync<UserFriendlyException>(() => escalationAppService.ApproveAsync(item.Id));

        var repository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var reloaded = await repository.GetAsync(item.Id);
        Assert.Equal(EscalationItemStatus.Pending, reloaded.Status);
    }

    [Fact]
    public async Task Approve_When_Handler_ReValidation_Fails_Marks_Item_Failed_With_ExecutionError_Instead_Of_Throwing()
    {
        var customerId = await SeedCustomerAsync();
        var quoteId = await SeedThinMarginQuoteAsync(customerId);

        // Move the quote to Sent through the normal granted path first (AlwaysAllow means this
        // works), so the escalation's later re-validation (SendAsync's Status != Draft guard)
        // fails on approval - simulating "state drifted since filing."
        var quoteAppService = GetRequiredService<QuoteAppService>();
        await quoteAppService.SendAsync(quoteId, "Already sent via the normal path");

        var item = await SeedEscalationAsync("Quote.MarginOverride", "Quote", quoteId, "Stale request");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await escalationAppService.ApproveAsync(item.Id); // must not throw

        var repository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var reloaded = await repository.GetAsync(item.Id);
        Assert.Equal(EscalationItemStatus.Failed, reloaded.Status);
        Assert.False(string.IsNullOrWhiteSpace(reloaded.ExecutionError));
    }

    [Fact]
    public async Task Approving_A_NonPending_Item_Throws()
    {
        var customerId = await SeedCustomerAsync();
        var quoteId = await SeedThinMarginQuoteAsync(customerId);
        var item = await SeedEscalationAsync("Quote.MarginOverride", "Quote", quoteId, "reason");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await escalationAppService.ApproveAsync(item.Id);

        await Assert.ThrowsAsync<UserFriendlyException>(() => escalationAppService.ApproveAsync(item.Id));
    }

    [Fact]
    public async Task Reject_Sets_Status_And_Notes()
    {
        await EnsureDatabaseCreatedAsync();
        var item = await SeedEscalationAsync("Quote.MarginOverride", "Quote", Guid.NewGuid(), "reason");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await escalationAppService.RejectAsync(item.Id, "Not approved this quarter");

        var repository = GetRequiredService<IRepository<EscalationItem, Guid>>();
        var reloaded = await repository.GetAsync(item.Id);
        Assert.Equal(EscalationItemStatus.Rejected, reloaded.Status);
        Assert.Equal("Not approved this quarter", reloaded.DecisionNotes);
    }

    [Fact]
    public async Task GetListAsync_Filters_By_Status()
    {
        await EnsureDatabaseCreatedAsync();
        var pending = await SeedEscalationAsync("Quote.MarginOverride", "Quote", Guid.NewGuid(), "pending one");
        var rejected = await SeedEscalationAsync("Quote.MarginOverride", "Quote", Guid.NewGuid(), "rejected one");

        var escalationAppService = GetRequiredService<EscalationItemAppService>();
        await escalationAppService.RejectAsync(rejected.Id, "not approved");

        var pendingResult = await escalationAppService.GetListAsync(new GetEscalationItemListInput { Status = EscalationItemStatus.Pending });
        Assert.Equal(1, pendingResult.TotalCount);
        Assert.Equal(pending.Id, pendingResult.Items[0].Id);

        var rejectedResult = await escalationAppService.GetListAsync(new GetEscalationItemListInput { Status = EscalationItemStatus.Rejected });
        Assert.Equal(1, rejectedResult.TotalCount);
        Assert.Equal(rejected.Id, rejectedResult.Items[0].Id);
    }
}
