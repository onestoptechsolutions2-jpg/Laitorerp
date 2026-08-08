using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

// Resolves a Product's suggested UnitPrice against a price list, falling back to the product's
// standard price - same "suggest, don't lock" pattern as TaxRateResolver, but for price. Only
// consulted when the client submits UnitPrice == 0 (the untouched-form default), so it never
// overwrites a price a user actually typed in - see QuoteLineAppService/OrderLineAppService.
public static class PriceListResolver
{
    public static async Task<decimal> ResolveUnitPriceAsync(
        IRepository<PriceListItem, Guid> priceListItemRepository,
        IRepository<Product, Guid> productRepository,
        Guid? priceListId,
        Guid productId)
    {
        if (priceListId.HasValue)
        {
            var priceListItem = (await priceListItemRepository.GetListAsync(
                x => x.PriceListId == priceListId.Value && x.ProductId == productId)).FirstOrDefault();
            if (priceListItem != null)
            {
                return priceListItem.UnitPrice;
            }
        }

        var product = await productRepository.GetAsync(productId);
        return product.UnitPrice;
    }
}