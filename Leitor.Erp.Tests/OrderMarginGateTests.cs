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

// The Order-side half of the margin gate ([[feature_quote_margin_gate_2026-08-15]]) - most Orders
// inherit an already-gated margin from the Quote they were converted from, so this covers an Order
// built directly with its own lines, which never went through a gated Quote. Same test shape as
// QuoteMarginGateTests; see that file's header comment for the CurrentUser.Id caveat in this host,
// and for the escalation-filing branch (untestable here for the same reason) now covered by
// EscalationItemTests.cs instead.
public class OrderMarginGateTests : ErpTestBase
{
    private async Task<(OrderAppService OrderAppService, OrderLineAppService LineAppService, Guid CustomerId)> SeedAsync()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Order Margin Test Ltd" });

        return (
            GetRequiredService<OrderAppService>(),
            GetRequiredService<OrderLineAppService>(),
            customer.Id
        );
    }

    private async Task<OrderDto> CreateSubmittedOrderAsync(OrderAppService orderAppService, OrderLineAppService lineAppService, Guid customerId, decimal unitPrice, decimal cost)
    {
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
            UnitPrice = unitPrice,
            Quantity = 1,
            Cost = cost
        });

        return order;
    }

    [Fact]
    public async Task Confirming_A_BelowFloor_Order_Without_Override_Throws()
    {
        var (orderAppService, lineAppService, customerId) = await SeedAsync();

        // UnitPrice 100, Cost 92 -> 8% margin, below the 15% default floor.
        var order = await CreateSubmittedOrderAsync(orderAppService, lineAppService, customerId, unitPrice: 100m, cost: 92m);

        await Assert.ThrowsAsync<UserFriendlyException>(() => orderAppService.ConfirmAsync(order.Id));
    }

    [Fact]
    public async Task Confirming_A_BelowFloor_Order_With_Override_Reason_Succeeds_And_Stamps_Audit_Fields()
    {
        var (orderAppService, lineAppService, customerId) = await SeedAsync();

        var order = await CreateSubmittedOrderAsync(orderAppService, lineAppService, customerId, unitPrice: 100m, cost: 92m);

        await orderAppService.ConfirmAsync(order.Id, "Founder approved - strategic account");

        var confirmed = await orderAppService.GetAsync(order.Id);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
        Assert.Equal("Founder approved - strategic account", confirmed.MarginOverrideReason);
        Assert.NotNull(confirmed.MarginOverrideAt);
    }

    [Fact]
    public async Task Confirming_An_AtOrAboveFloor_Order_Succeeds_Without_Override_Reason()
    {
        var (orderAppService, lineAppService, customerId) = await SeedAsync();

        // UnitPrice 100, Cost 50 -> 50% margin, comfortably above the 15% default floor.
        var order = await CreateSubmittedOrderAsync(orderAppService, lineAppService, customerId, unitPrice: 100m, cost: 50m);

        await orderAppService.ConfirmAsync(order.Id);

        var confirmed = await orderAppService.GetAsync(order.Id);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
        Assert.Null(confirmed.MarginOverrideReason);
    }
}
