using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Services.ServiceCatalog;
using Volo.Abp;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers rate cards (2026-08-17): PriceListItem extended to price a ServiceCatalogItem as an
// alternative to a Product (Fixed or Hourly), and Quote/OrderLine gained ServiceCatalogItemId so
// a line can reference a service the same way it already could a product. "Per scope" callouts
// stay a plain manual line - deliberately not covered here, nothing changed for that path.
public class RateCardTests : ErpTestBase
{
    private async Task<Guid> CreateServiceAsync(string name)
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ServiceCatalog, "true", "T", null);

        var serviceCatalogItemAppService = GetRequiredService<ServiceCatalogItemAppService>();
        var service = await serviceCatalogItemAppService.CreateAsync(new CreateUpdateServiceCatalogItemDto { Name = name });
        return service.Id;
    }

    [Fact]
    public async Task PriceListItem_Requires_Exactly_One_Of_Product_Or_Service()
    {
        await EnsureDatabaseCreatedAsync();
        var priceListAppService = GetRequiredService<PriceListAppService>();
        var priceList = await priceListAppService.CreateAsync(new CreateUpdatePriceListDto { Name = "Test List" });

        var priceListItemAppService = GetRequiredService<PriceListItemAppService>();

        // Neither set.
        await Assert.ThrowsAsync<UserFriendlyException>(() => priceListItemAppService.CreateAsync(new CreateUpdatePriceListItemDto
        {
            PriceListId = priceList.Id,
            UnitPrice = 100m
        }));

        var serviceId = await CreateServiceAsync("Network Infrastructure Management");
        var productAppService = GetRequiredService<ProductAppService>();
        var product = await productAppService.CreateAsync(new CreateUpdateProductDto { Name = "Router", UnitPrice = 50m });

        // Both set.
        await Assert.ThrowsAsync<UserFriendlyException>(() => priceListItemAppService.CreateAsync(new CreateUpdatePriceListItemDto
        {
            PriceListId = priceList.Id,
            ProductId = product.Id,
            ServiceCatalogItemId = serviceId,
            UnitPrice = 100m
        }));
    }

    [Fact]
    public async Task PriceListItem_For_A_Service_Stores_RateType()
    {
        await EnsureDatabaseCreatedAsync();
        var priceListAppService = GetRequiredService<PriceListAppService>();
        var priceList = await priceListAppService.CreateAsync(new CreateUpdatePriceListDto { Name = "Callout Rates" });
        var serviceId = await CreateServiceAsync("User & Device Support");

        var priceListItemAppService = GetRequiredService<PriceListItemAppService>();
        var item = await priceListItemAppService.CreateAsync(new CreateUpdatePriceListItemDto
        {
            PriceListId = priceList.Id,
            ServiceCatalogItemId = serviceId,
            UnitPrice = 5000m,
            RateType = RateType.Hourly
        });

        var reloaded = await priceListItemAppService.GetAsync(item.Id);
        Assert.Equal(RateType.Hourly, reloaded.RateType);
        Assert.Equal("User & Device Support", reloaded.ServiceCatalogItemName);
        Assert.Null(reloaded.ProductId);
    }

    [Fact]
    public async Task Adding_A_Quote_Line_With_A_Service_Resolves_The_Rate_Card_Price()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var priceListAppService = GetRequiredService<PriceListAppService>();
        var priceList = await priceListAppService.CreateAsync(new CreateUpdatePriceListDto { Name = "Standard Rates" });

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Wayne Enterprises",
            DefaultPriceListId = priceList.Id
        });

        var serviceId = await CreateServiceAsync("Endpoint Security");
        var priceListItemAppService = GetRequiredService<PriceListItemAppService>();
        await priceListItemAppService.CreateAsync(new CreateUpdatePriceListItemDto
        {
            PriceListId = priceList.Id,
            ServiceCatalogItemId = serviceId,
            UnitPrice = 15000m,
            RateType = RateType.Fixed
        });

        var quoteAppService = GetRequiredService<QuoteAppService>();
        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customer.Id,
            Title = "Security Retainer",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var quoteLineAppService = GetRequiredService<QuoteLineAppService>();
        var line = await quoteLineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            ServiceCatalogItemId = serviceId,
            Description = "Endpoint Security",
            Quantity = 1
            // UnitPrice deliberately left at 0 (the untouched-form default) to exercise resolution.
        });

        Assert.Equal(15000m, line.UnitPrice);
        Assert.Equal(serviceId, line.ServiceCatalogItemId);
    }

    [Fact]
    public async Task Adding_An_Order_Line_With_An_Hourly_Service_And_No_RateCard_Match_Leaves_Price_As_Typed()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var priceListAppService = GetRequiredService<PriceListAppService>();
        var priceList = await priceListAppService.CreateAsync(new CreateUpdatePriceListDto { Name = "Empty List" });

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Ad-hoc Client",
            DefaultPriceListId = priceList.Id
        });

        var serviceId = await CreateServiceAsync("Incident Response");

        var orderAppService = GetRequiredService<OrderAppService>();
        var order = await orderAppService.CreateAsync(new CreateUpdateOrderDto
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var orderLineAppService = GetRequiredService<OrderLineAppService>();
        var line = await orderLineAppService.CreateAsync(new CreateUpdateOrderLineDto
        {
            OrderId = order.Id,
            ServiceCatalogItemId = serviceId,
            Description = "Emergency callout - 3 hours",
            UnitPrice = 6000m, // No rate-card match for this price list, so this typed value stays.
            Quantity = 3
        });

        Assert.Equal(6000m, line.UnitPrice);
    }
}
