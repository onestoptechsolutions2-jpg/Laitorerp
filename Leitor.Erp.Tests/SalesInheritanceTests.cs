using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers Phase 3's CRM/Sales inheritance fixes: price-list-aware line pricing, customer-change
// re-pricing, the credit limit gate, and salesperson attribution carried through Quote -> Order.
public class SalesInheritanceTests : ErpTestBase
{
    private async Task<Guid> SeedProductAsync()
    {
        await EnsureDatabaseCreatedAsync();

        var productRepository = GetRequiredService<IRepository<Product, Guid>>();
        var product = new Product(Guid.NewGuid(), "Wireless Access Point", 100m);
        await productRepository.InsertAsync(product, autoSave: true);
        return product.Id;
    }

    private async Task<Guid> SeedCustomerWithPriceListAsync(Guid productId, decimal listPrice, decimal discountPercent)
    {
        var priceListRepository = GetRequiredService<IRepository<PriceList, Guid>>();
        var priceListItemRepository = GetRequiredService<IRepository<PriceListItem, Guid>>();
        var customerAppService = GetRequiredService<CustomerAppService>();

        var priceList = new PriceList(Guid.NewGuid(), "VIP Pricing");
        await priceListRepository.InsertAsync(priceList, autoSave: true);

        var priceListItem = new PriceListItem(Guid.NewGuid(), priceList.Id, listPrice) { ProductId = productId };
        await priceListItemRepository.InsertAsync(priceListItem, autoSave: true);

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Priced Customer",
            DefaultPriceListId = priceList.Id,
            DiscountPercent = discountPercent
        });

        return customer.Id;
    }

    [Fact]
    public async Task QuoteLine_With_Zero_UnitPrice_Resolves_From_Customer_PriceList()
    {
        var productId = await SeedProductAsync();
        var customerId = await SeedCustomerWithPriceListAsync(productId, listPrice: 85m, discountPercent: 5m);

        var quoteAppService = GetRequiredService<QuoteAppService>();
        var quoteLineAppService = GetRequiredService<QuoteLineAppService>();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customerId,
            Title = "VIP Quote",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var line = await quoteLineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            ProductId = productId,
            Description = "Wireless Access Point",
            UnitPrice = 0,
            Quantity = 1
        });

        Assert.Equal(85m, line.UnitPrice);
        Assert.Equal(5m, line.DiscountPercent);
    }

    [Fact]
    public async Task Quote_CustomerChange_Reprices_Existing_Lines()
    {
        var productId = await SeedProductAsync();
        var customerAId = await SeedCustomerWithPriceListAsync(productId, listPrice: 85m, discountPercent: 5m);
        var customerBId = await SeedCustomerWithPriceListAsync(productId, listPrice: 60m, discountPercent: 20m);

        var quoteAppService = GetRequiredService<QuoteAppService>();
        var quoteLineAppService = GetRequiredService<QuoteLineAppService>();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customerAId,
            Title = "Switching Customers",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var line = await quoteLineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            ProductId = productId,
            Description = "Wireless Access Point",
            UnitPrice = 0,
            Quantity = 1
        });
        Assert.Equal(85m, line.UnitPrice);

        await quoteAppService.UpdateAsync(quote.Id, new CreateUpdateQuoteDto
        {
            CustomerId = customerBId,
            Title = quote.Title,
            Status = quote.Status,
            IssueDate = quote.IssueDate,
            CurrencyCode = quote.CurrencyCode
        });

        var repriced = await quoteLineAppService.GetAsync(line.Id);
        Assert.Equal(60m, repriced.UnitPrice);
        Assert.Equal(20m, repriced.DiscountPercent);
    }

    [Fact]
    public async Task OrderConfirm_Throws_When_Exceeding_CreditLimit()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var orderAppService = GetRequiredService<OrderAppService>();
        var orderLineAppService = GetRequiredService<OrderLineAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Tight Budget Ltd",
            CreditLimit = 100m
        });

        var order = await orderAppService.CreateAsync(new CreateUpdateOrderDto
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        await orderLineAppService.CreateAsync(new CreateUpdateOrderLineDto
        {
            OrderId = order.Id,
            Description = "Over-limit line",
            UnitPrice = 500m,
            Quantity = 1
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => orderAppService.ConfirmAsync(order.Id));
    }

    [Fact]
    public async Task ConvertToOrder_Carries_SalespersonUserId_Forward()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var quoteAppService = GetRequiredService<QuoteAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Attribution Test Co" });
        var salespersonId = Guid.NewGuid();

        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customer.Id,
            Title = "Attributed Quote",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES",
            SalespersonUserId = salespersonId
        });
        Assert.Equal(salespersonId, quote.SalespersonUserId);

        var order = await quoteAppService.ConvertToOrderAsync(quote.Id);

        Assert.Equal(salespersonId, order.SalespersonUserId);
    }
}